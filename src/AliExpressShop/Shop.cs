using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MultiShop.ShopFramework;
using SimpleLogger;

namespace AliExpressShop
{
    public class Shop : IShop
    {
        private const string ALIEXPRESS_BASE_URL = "https://www.aliexpress.com";
        private const string ALIEXPRESS_QUERY_FORMAT = "/wholesale?trafficChannel=main&d=y&CatId=0&SearchText={0}&ltype=wholesale&SortType=default&page={1}";
        private const char SPACE_CHAR = '+';
        private const int DELAY = 500;

        //Regex
        private Regex dataLineRegex = new Regex("^ +window.runParams = .+\"items\":.+;$");
        private Regex pageCountRegex = new Regex("\"maxPage\":(\\d+)");
        private Regex itemRatingRegex = new Regex("\"starRating\":\"(\\d*.\\d*)\"");
        private Regex itemsSoldRegex = new Regex("\"tradeDesc\":\"(\\d+) sold\"");
        private const string SHIPPING_REGEX_FORMAT = "Shipping: {0} ?\\$ (\\d*.\\d*)";
        private Regex shippingPriceRegex;
        private readonly string freeShippingStr = "\"logisticsDesc\":\"Free Shipping\"";
        private const string PRICE_REGEX_FORMAT = "\"price\":\"{0} ?\\$ ?(\\d*.\\d*)( - (\\d+.\\d+))?\",";
        private Regex itemPriceRegex;

        //Sequences
        private const string ITEM_LIST_SEQ = "\"items\":";
        private const string TITLE_SEQ = "\"title\":";
        private const string IMAGE_URL_SEQ = "\"imageUrl\":";
        private const string PRODUCT_URL_SEQ = "\"productDetailUrl\":";
        private HttpClientHandler handler;
        private HttpClient client;
        private bool disposedValue;

        public string ShopName => "AliExpress";

        public string ShopDescription => "A China based online store.";

        public string ShopModuleAuthor => null;

        public void Initiate(Currency currency)
        {
            if (disposedValue) throw new ObjectDisposedException("Shop");
            if (client != null) throw new InvalidOperationException("Already initiated.");
            Logger.AddLogListener(new ConsoleLogReceiver());
            itemPriceRegex = new Regex(string.Format(PRICE_REGEX_FORMAT, CurrencyToDisplayStr(currency)));
            shippingPriceRegex = new Regex(string.Format(SHIPPING_REGEX_FORMAT, CurrencyToDisplayStr(currency)));

            Uri baseAddress = new Uri(ALIEXPRESS_BASE_URL);
            CookieContainer container = new CookieContainer();
            handler = new HttpClientHandler();
            handler.CookieContainer = container;
            client = new HttpClient(handler);
            client.BaseAddress = baseAddress;
            client.Send(new HttpRequestMessage());
            container.Add(baseAddress, new Cookie("aep_usuc_f", string.Format("site=glo&c_tp={0}&region=CA&b_locale=en_US", currency)));
        }

        public async Task<IEnumerable<ProductListing>> Search(string query, int maxPage = -1)
        {
            if (disposedValue) throw new ObjectDisposedException("Shop");
            if (client == null) throw new InvalidOperationException("HTTP client is not initiated.");
            List<ProductListing> listings = new List<ProductListing>();

            string modifiedQuery = query.Replace(' ', SPACE_CHAR);
            Logger.Log($"Searching {ShopName} with query \"{query}\".", LogLevel.INFO);

            int? length = null;
            for (int i = 1; i <= (length != null ? length : 1); i++)
            {
                if (maxPage != -1 && i > maxPage) break;
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, string.Format(ALIEXPRESS_QUERY_FORMAT, modifiedQuery, i));
                Logger.Log($"Sending GET request with uri: {request.RequestUri}", LogLevel.DEBUG);
                HttpResponseMessage response = await client.SendAsync(request);
                
                string data = null;
                using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    string line = null;
                    while ((line = await reader.ReadLineAsync()) != null && (data == null || length == null))
                    {
                        if (dataLineRegex.IsMatch(line)) {
                            data = line.Trim();
                            Logger.Log($"Found line with listing data.", LogLevel.DEBUG);
                        } else if (length == null && pageCountRegex.IsMatch(line)) {
                            Match match = pageCountRegex.Match(line);
                            length = int.Parse(match.Groups[1].Captures[0].Value);
                            Logger.Log($"Found {length} pages.", LogLevel.DEBUG);
                        }
                    }
                }
                if (data == null) return listings;
                string itemsString = GetBracketSet(data, data.IndexOf(ITEM_LIST_SEQ) + ITEM_LIST_SEQ.Length, '[', ']');
                IEnumerable<string> listingsStrs = GetItemsFromString(itemsString);
                foreach (string listingStr in listingsStrs)
                {
                    listings.Add(GenerateListingFromString(listingStr));
                }
                Logger.Log($"Delaying next page by {DELAY}ms.", LogLevel.DEBUG);
                await Task.Delay(DELAY);
            }
            return listings;
        }

        private ProductListing GenerateListingFromString(string str) {
            ProductListing listing = new ProductListing();
            string name = GetQuoteSet(str, str.IndexOf(TITLE_SEQ) + TITLE_SEQ.Length);
            if (name != null) {
                Logger.Log($"Found name: {name}", LogLevel.DEBUG);
                listing.Name = name;
            } else {
                Logger.Log($"Unable to get listing name from: \n {str}", LogLevel.WARNING);
            }
            Match ratingMatch = itemRatingRegex.Match(str);
            if (ratingMatch.Success) {
                Logger.Log($"Found rating: {ratingMatch.Groups[1].Value}", LogLevel.DEBUG);
                listing.Rating = float.Parse(ratingMatch.Groups[1].Value);
            }
            Match numberSoldMatch = itemsSoldRegex.Match(str);
            if (numberSoldMatch.Success) {
                Logger.Log($"Found quantity sold: {numberSoldMatch.Groups[1].Value}", LogLevel.DEBUG);
                listing.PurchaseCount = int.Parse(numberSoldMatch.Groups[1].Value);
            }

            Match priceMatch = itemPriceRegex.Match(str);
            if (priceMatch.Success) {
                Logger.Log($"Found price: {priceMatch.Groups[1].Value}", LogLevel.DEBUG);
                listing.LowerPrice = float.Parse(priceMatch.Groups[1].Value);
                if (priceMatch.Groups[3].Success) {
                    Logger.Log($"Found a price range: {priceMatch.Groups[3].Value}", LogLevel.DEBUG);
                    listing.UpperPrice = float.Parse(priceMatch.Groups[3].Value);
                } else {
                    listing.UpperPrice = listing.LowerPrice.Value;
                }
            } else {
                Logger.Log($"Unable to get listing price from: \n {str}", LogLevel.WARNING);
            }

            string prodUrl = GetQuoteSet(str, str.IndexOf(PRODUCT_URL_SEQ) + PRODUCT_URL_SEQ.Length).Substring(2);
            if (prodUrl != null) {
                Logger.Log($"Found URL: {prodUrl}", LogLevel.DEBUG);
                listing.URL = prodUrl;
            } else {
                Logger.Log($"Unable to get item URL from: \n {str}", LogLevel.WARNING);
            }
            string imageUrl = GetQuoteSet(str, str.IndexOf(IMAGE_URL_SEQ) + IMAGE_URL_SEQ.Length).Substring(2);
            if (imageUrl != null) {
                Logger.Log($"Found image URL: {imageUrl}", LogLevel.DEBUG);
                listing.ImageURL = imageUrl;
            }
            Match shippingMatch = shippingPriceRegex.Match(str);
            if (shippingMatch.Success) {
                Logger.Log($"Found shipping price: {shippingMatch.Groups[1].Value}", LogLevel.DEBUG);
                listing.Shipping = float.Parse(shippingMatch.Groups[1].Value);
            } else if (str.Contains(freeShippingStr)) {
                listing.Shipping = 0;
            } else {
                listing.Shipping = null;
            }
            return listing;
        }

        private string GetQuoteSet(string str, int start) {
            char[] cs = str.ToCharArray();
            int quoteCount = 0;
            int a = -1;
            if (start < 0) return null;
            for (int b = start; b < cs.Length; b++)
            {
                if (cs[b] == '"' && !(b >= 1 && cs[b - 1] == '\\')) {
                    if (a == -1) {
                        a = b + 1;
                    }
                    quoteCount += 1;

                    if (quoteCount >= 2) {
                        return str.Substring(a, b - a); 
                    }
                }
            }
            return null;
        }

        private string GetBracketSet(string str, int start, char open = '{', char close = '}') {
            if (start < 0) return null;

            char[] cs = str.ToCharArray();
            int bracketDepth = 0;
            int a = -1;
            for (int i = start; i < cs.Length; i++)
            {
                char c = cs[i];
                if (c == open) {
                    if (a < 0) {
                        a = i;
                    }
                    bracketDepth += 1;
                } else if (c == close) {
                    bracketDepth -= 1;
                    if (bracketDepth == 0) {
                        if (i + 1 >= cs.Length) {
                            return str.Substring(a);
                        }
                        return str.Substring(a, i - a + 1);
                    } else if (bracketDepth < 0) {
                        return null;
                    }
                }
            }
            return null;
        }

        private IEnumerable<string> GetItemsFromString(string str) {
            int startPos = 0;
            string itemString = null;
            while ((itemString = GetBracketSet(str, startPos)) != null)
            {
                startPos += itemString.Length + 1;
                yield return itemString;
            }
        }

        private string CurrencyToDisplayStr(Currency currency) {
            switch (currency)
            {
                case Currency.CAD:
                    return "C";
                case Currency.USD:
                    return "US";
                default:
                    throw new InvalidOperationException($"Currency \"{currency}\" is not supported.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                    handler.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
