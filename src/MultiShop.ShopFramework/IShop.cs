using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiShop.ShopFramework
{
    public interface IShop : IDisposable
    {
        string ShopName { get; }
        string ShopDescription { get; }
        string ShopModuleAuthor { get; }
        
        void Initiate(Currency currency);
        Task<IEnumerable<ProductListing>> Search(string query, int maxPage = -1);
    }
}