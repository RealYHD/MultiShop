using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using MultiShop.ShopFramework;

namespace BanggoodShop
{
    public class Shop : IShop
    {
        public bool UseProxy { get; set; } = true;
        private bool disposedValue;

        public string ShopName => "Banggood";

        public string ShopDescription => "A online retailer based in China.";

        public string ShopModuleAuthor => "Reslate";

        private HttpClient http;
        private string query;
        private Currency currency;

        public IAsyncEnumerator<ProductListing> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new ShopEnumerator(query, currency, http, UseProxy, cancellationToken);
        }

        public void Initialize()
        {
            this.http = new HttpClient();
        }

        public void SetupSession(string query, Currency currency)
        {
            this.query = query;
            this.currency = currency;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    http.Dispose();
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
