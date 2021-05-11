using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using MultiShop.ShopFramework;
using SimpleLogger;

namespace BanggoodShop
{
    class ShopEnumerator : IAsyncEnumerator<ProductListing>
    {
        const string PROXY_FORMAT = "https://cors.bridged.cc/{0}";
        private const string QUERY_FORMAT = "https://www.banggood.com/search/{0}/0-0-0-1-1-60-0-price-0-0_p-{1}.html?DCC=CA&currency={2}";
        HttpClient http;
        private string query;
        private Currency currency;
        private bool useProxy;
        private CancellationToken cancellationToken;

        private IEnumerator<ProductListing> pageListings;
        private int currentPage;
        private DateTime lastScrape;

        public ProductListing Current { get; private set; }

        public ShopEnumerator(string query, Currency currency, HttpClient http, bool useProxy, CancellationToken cancellationToken)
        {
            query = query.Replace(' ', '-');
            this.query = query;
            this.currency = currency;
            this.http = http;
            this.useProxy = useProxy;
            this.cancellationToken = cancellationToken;
        }

        private async Task<IEnumerable<ProductListing>> ScrapePage(int page)
        {
            string requestUrl = string.Format(QUERY_FORMAT, query, page, currency.ToString());
            if (useProxy) requestUrl = string.Format(PROXY_FORMAT, requestUrl);
            TimeSpan difference = DateTime.Now - lastScrape;
            if (difference.TotalMilliseconds < 200) {
                await Task.Delay((int)Math.Ceiling(200 - difference.TotalMilliseconds));
            }
            HttpResponseMessage response = await http.GetAsync(requestUrl);
            lastScrape = DateTime.Now;
            HtmlDocument html = new HtmlDocument();
            html.Load(await response.Content.ReadAsStreamAsync());
            HtmlNodeCollection collection = html.DocumentNode.SelectNodes(@"//div[@class='product-list']/ul[@class='goodlist cf']/li");
            if (collection == null) return null;
            List<ProductListing> results = new List<ProductListing>();
            foreach (HtmlNode node in collection)
            {
                ProductListing listing = new ProductListing();
                HtmlNode productNode = node.SelectSingleNode(@"div/a[1]");
                listing.Name = productNode.InnerText;
                Logger.Log($"Found name: {listing.Name}", LogLevel.Debug);
                listing.URL = productNode.GetAttributeValue("href", null);
                Logger.Log($"Found URL: {listing.URL}", LogLevel.Debug);
                listing.ImageURL = node.SelectSingleNode(@"div/span[@class='img notranslate']/a/img").GetAttributeValue("data-src", null);
                Logger.Log($"Found image URL: {listing.ImageURL}", LogLevel.Debug);
                listing.LowerPrice = float.Parse(Regex.Match(node.SelectSingleNode(@"div/span[@class='price-box']/span").InnerText, @"(\d*\.\d*)").Groups[1].Value);
                Logger.Log($"Found price: {listing.LowerPrice}", LogLevel.Debug);
                listing.UpperPrice = listing.LowerPrice;
                listing.ReviewCount = int.Parse(Regex.Match(node.SelectSingleNode(@"div/a[2]").InnerText, @"(\d+) reviews?").Groups[1].Value);
                Logger.Log($"Found reviews: {listing.ReviewCount}", LogLevel.Debug);
                results.Add(listing);
            }
            return results;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (pageListings == null || !pageListings.MoveNext())
            {
                currentPage += 1;
                pageListings?.Dispose();
                IEnumerable<ProductListing> pageEnumerable = await ScrapePage(currentPage);
                if (pageEnumerable == null) return false;
                pageListings = pageEnumerable.GetEnumerator();
                pageListings.MoveNext();
            }
            Current = pageListings.Current;
            return true;
        }
    }
}