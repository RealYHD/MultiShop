using System.Collections.Generic;
using System.Threading.Tasks;
using GameServiceWarden.Core.Tests;
using MultiShop.ShopFramework;
using SimpleLogger;
using Xunit;
using Xunit.Abstractions;

namespace AliExpressShop.Tests
{
    public class ShopTest
    {
        public ShopTest(ITestOutputHelper output)
        {
            Logger.AddLogListener(new XUnitLogger(output));
        }

        [Fact]
        public void Search_SearchForItem_ImportantInfoFound()
        {
            //Given
            Shop shop = new Shop();
            shop.Initiate(Currency.CAD);
            //When
            Task<IEnumerable<ProductListing>> listingsTask = shop.Search("mpu6050", 1);
            listingsTask.Wait();
            IEnumerable<ProductListing> listings = listingsTask.Result;
            Assert.NotEmpty(listings);
            foreach (ProductListing listing in listings)
            {
                Assert.False(string.IsNullOrWhiteSpace(listing.Name));
                Assert.True(listing.LowerPrice != 0);
            }
        }

        [Fact]
        public void Search_SearchForItem_MultiplePages()
        {
            //Given
            Shop shop = new Shop();
            shop.Initiate(Currency.CAD);
            //When
            Task<IEnumerable<ProductListing>> listingsTask = shop.Search("mpu6050", 2);
            listingsTask.Wait();
            IEnumerable<ProductListing> listings = listingsTask.Result;
            //Then
            Assert.NotEmpty(listings);
            foreach (ProductListing listing in listings)
            {
                Assert.False(string.IsNullOrWhiteSpace(listing.Name));
            }
        }

        [Fact]
        public void Search_USD_ResultsFound()
        {
            //Given
            Shop shop = new Shop();
            shop.Initiate(Currency.USD);
            //When
            Task<IEnumerable<ProductListing>> listingsTask = shop.Search("mpu6050", 1);
            listingsTask.Wait();
            IEnumerable<ProductListing> listings = listingsTask.Result;
            //Then
            Assert.NotEmpty(listings);
            foreach (ProductListing listing in listings)
            {
                Assert.False(string.IsNullOrWhiteSpace(listing.Name));
                Assert.True(listing.LowerPrice != 0);
            }
        }
    }
}
