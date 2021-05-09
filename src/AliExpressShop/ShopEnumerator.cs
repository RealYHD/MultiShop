using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GameServiceWarden.Core.Collection;
using MultiShop.ShopFramework;
using SimpleLogger;

namespace AliExpressShop
{
    class ShopEnumerator : IAsyncEnumerator<ProductListing>
    {
        private LRUCache<(string, Currency), float> conversionCache = new LRUCache<(string, Currency), float>();
        private string query;
        private Currency currency;
        private HttpClient http;
        bool useProxy;
        int currentPage = 0;
        IEnumerator<ProductListing> pageListings;
        private bool disposedValue;

        public ProductListing Current {get; private set;}

        public ShopEnumerator(string query, Currency currency, HttpClient http, bool useProxy = true)
        {
            this.query = query;
            this.currency = currency;
            this.http = http;
            this.useProxy = useProxy;
        }


        private async Task<IEnumerable<ProductListing>> ScrapePage(int page)
        {
            const string ALIEXPRESS_QUERY_FORMAT = "https://www.aliexpress.com/wholesale?trafficChannel=main&d=y&CatId=0&SearchText={0}&ltype=wholesale&SortType=default&page={1}";
            const char SPACE_CHAR = '+';
            const string PROXY_FORMAT = "https://cors.bridged.cc/{0}";
            const int DELAY = 1000/5;
            Regex dataLineRegex = new Regex("^ +window.runParams = .+\"items\":.+;$");
            Regex pageCountRegex = new Regex("\"maxPage\":(\\d+)");
            const string ITEM_LIST_SEQ = "\"items\":";


            if (http == null) throw new InvalidOperationException("HttpClient is not initiated.");
            List<ProductListing> listings = new List<ProductListing>();

            string modifiedQuery = query.Replace(' ', SPACE_CHAR);
            Logger.Log($"Searching with query \"{query}\".", LogLevel.Info);

            DateTime start = DateTime.Now;
            //Set up request. We need to use the Cors Proxy.
            string url = string.Format(ALIEXPRESS_QUERY_FORMAT, modifiedQuery, page);
            HttpRequestMessage request = null;
            if (useProxy) {
                request = new HttpRequestMessage(HttpMethod.Get, string.Format(PROXY_FORMAT, url));
            } else {
                request = new HttpRequestMessage(HttpMethod.Get, url);
            }

            //Delay for Cors proxy.
            double waitTime = DELAY - (DateTime.Now - start).TotalMilliseconds;
            if (waitTime > 0) {
                Logger.Log($"Delaying next page by {waitTime}ms.", LogLevel.Debug);
                await Task.Delay((int)Math.Ceiling(waitTime));
            }

            Logger.Log($"Sending GET request with uri: {request.RequestUri}", LogLevel.Debug);
            HttpResponseMessage response = await http.SendAsync(request);
            start = DateTime.Now;
            
            string data = null;
            using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                string line = null;
                while ((line = await reader.ReadLineAsync()) != null && data == null)
                {
                    if (dataLineRegex.IsMatch(line)) {
                        data = line.Trim();
                        Logger.Log($"Found line with listing data.", LogLevel.Debug);
                    }
                }
            }
            if (data == null) {
                Logger.Log($"Completed search prematurely with status {response.StatusCode} ({(int)response.StatusCode}).");
                return null;
            }
            string itemsString = GetBracketSet(data, data.IndexOf(ITEM_LIST_SEQ) + ITEM_LIST_SEQ.Length, '[', ']');
            IEnumerable<string> listingsStrs = GetItemsFromString(itemsString);
            foreach (string listingStr in listingsStrs)
            {
                listings.Add(await GenerateListingFromString(listingStr, currency));
            }
            return listings;
        }

        private async Task<ProductListing> GenerateListingFromString(string str, Currency currency) {
            Regex itemRatingRegex = new Regex("\"starRating\":\"(\\d*.\\d*)\"");
            Regex itemsSoldRegex = new Regex("\"tradeDesc\":\"(\\d+) sold\"");
            Regex shippingPriceRegex = new Regex("Shipping: \\w+ ?\\$ ?(\\d*.\\d*)");
            Regex itemPriceRegex = new Regex("\"price\":\"\\w+ ?\\$ ?(\\d*.\\d*)( - (\\d+.\\d+))?\",");

            const string FREE_SHIPPING_STR = "\"logisticsDesc\":\"Free Shipping\"";
            const string TITLE_SEQ = "\"title\":";
            const string IMAGE_URL_SEQ = "\"imageUrl\":";
            const string PRODUCT_URL_SEQ = "\"productDetailUrl\":";
            
            ProductListing listing = new ProductListing();

            string name = GetQuoteSet(str, str.IndexOf(TITLE_SEQ) + TITLE_SEQ.Length);
            if (name != null) {
                Logger.Log($"Found name: {name}", LogLevel.Debug);
                listing.Name = name;
            } else {
                Logger.Log($"Unable to get listing name from: \n {str}", LogLevel.Warning);
            }
            Match ratingMatch = itemRatingRegex.Match(str);
            if (ratingMatch.Success) {
                Logger.Log($"Found rating: {ratingMatch.Groups[1].Value}", LogLevel.Debug);
                listing.Rating = float.Parse(ratingMatch.Groups[1].Value) / 5f;
            }
            Match numberSoldMatch = itemsSoldRegex.Match(str);
            if (numberSoldMatch.Success) {
                Logger.Log($"Found quantity sold: {numberSoldMatch.Groups[1].Value}", LogLevel.Debug);
                listing.PurchaseCount = int.Parse(numberSoldMatch.Groups[1].Value);
            }

            Match priceMatch = itemPriceRegex.Match(str);
            if (priceMatch.Success) {
                listing.LowerPrice = (float)Math.Round(float.Parse(priceMatch.Groups[1].Value) * await conversionCache.UseAsync(("USD", currency), () => FetchConversion("USD", currency)), 2);
                Logger.Log($"Found price: {listing.LowerPrice}", LogLevel.Debug);
                if (priceMatch.Groups[3].Success) {
                    listing.UpperPrice = (float)Math.Round(float.Parse(priceMatch.Groups[3].Value) * await conversionCache.UseAsync(("USD", currency), () => FetchConversion("USD", currency)), 2);
                    Logger.Log($"Found a price range with upper bound: {listing.UpperPrice}", LogLevel.Debug);
                } else {
                    listing.UpperPrice = (float)Math.Round(listing.LowerPrice * await conversionCache.UseAsync(("USD", currency), () => FetchConversion("USD", currency)), 2);
                }
            } else {
                Logger.Log($"Unable to get listing price from: \n {str}", LogLevel.Warning);
            }

            string prodUrl = GetQuoteSet(str, str.IndexOf(PRODUCT_URL_SEQ) + PRODUCT_URL_SEQ.Length).Substring(2);
            if (prodUrl != null) {
                Logger.Log($"Found URL: {prodUrl}", LogLevel.Debug);
                listing.URL = "https://" + prodUrl;
            } else {
                Logger.Log($"Unable to get item URL from: \n {str}", LogLevel.Warning);
            }
            string imageUrl = GetQuoteSet(str, str.IndexOf(IMAGE_URL_SEQ) + IMAGE_URL_SEQ.Length).Substring(2);
            if (imageUrl != null) {
                Logger.Log($"Found image URL: {imageUrl}", LogLevel.Debug);
                listing.ImageURL = "https://" + imageUrl;
            }
            Match shippingMatch = shippingPriceRegex.Match(str);
            if (shippingMatch.Success) {
                listing.Shipping = (float)Math.Round(float.Parse(shippingMatch.Groups[1].Value) * await conversionCache.UseAsync(("USD", currency), () => FetchConversion("USD", currency)), 2);
                Logger.Log($"Found shipping price: {listing.Shipping}", LogLevel.Debug);
            } else if (str.Contains(FREE_SHIPPING_STR)) {
                listing.Shipping = 0;
            } else {
                listing.Shipping = null;
            }
            listing.ConvertedPrices = true;
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

        private async Task<float> FetchConversion(string from, Currency to) {
            if (from.Equals(to.ToString())) return 1;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, string.Format("https://api.exchangerate.host/convert?from={0}&to={1}", from, to));
            HttpResponseMessage response = await http.SendAsync(request);
            string results = null;
            using (StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                results = await reader.ReadToEndAsync();
            }
            Match match = Regex.Match(results, "\"result\":(\\d*.\\d*)");
            return float.Parse(match.Groups[1].Value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (pageListings == null || !pageListings.MoveNext()) {
                pageListings?.Dispose();
                currentPage += 1;
                IEnumerable<ProductListing> currentListings = await ScrapePage(currentPage);
                if (currentListings == null) {
                    return false;
                }
                pageListings = currentListings.GetEnumerator();
                pageListings.MoveNext();
            }
            Current = pageListings.Current;
            return true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}