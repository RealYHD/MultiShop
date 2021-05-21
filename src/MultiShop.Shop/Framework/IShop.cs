using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MultiShop.Shop.Framework
{
    public interface IShop : IAsyncEnumerable<ProductListing>, IDisposable
    {
        string ShopName { get; }
        string ShopDescription { get; }
        string ShopModuleAuthor { get; }

        public void SetupSession(string query, Currency currency);

        void Initialize();
    }
}