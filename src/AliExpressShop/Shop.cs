using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GameServiceWarden.Core.Collection;
using MultiShop.ShopFramework;
using SimpleLogger;

namespace AliExpressShop
{
    public class Shop : IShop
    {
        public string ShopName => "AliExpress";

        public string ShopDescription => "A China based online store.";

        public string ShopModuleAuthor => "Reslate";

        public bool UseProxy { get; set; } = true;

        private HttpClient http;
        private string query;
        private Currency currency;
        private bool disposedValue;


        public void Initialize()
        {
            if (http != null) throw new InvalidOperationException("HttpClient already instantiated.");
            this.http = new HttpClient();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (http == null) throw new InvalidOperationException("HttpClient not instantiated.");
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

        public void SetupSession(string query, Currency currency)
        {
            this.query = query;
            this.currency = currency;
        }

        public IAsyncEnumerator<ProductListing> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new ShopEnumerator(cancellationToken, query, currency, http, UseProxy);
        }
    }
}
