using MultiShop.Shop.Framework;
using SimpleLogger;
using Xunit;
using Xunit.Abstractions;

namespace MultiShop.Shop.AliExpressModule.Tests
{
    public class ShopTest
    {
        public ShopTest(ITestOutputHelper output)
        {
            Logger.AddLogListener(new XUnitLogger(output));
        }

        [Fact]
        public async void Search_SearchForItem_MultiplePages()
        {
            //Given
            const int MAX_RESULTS = 120;
            Shop shop = new Shop();
            shop.UseProxy = false;
            shop.Initialize();
            //When
            shop.SetupSession("mpu6050", Currency.CAD);
            //Then
            int count = 0;
            await foreach (ProductListing listing in shop)
            {
                Assert.False(string.IsNullOrWhiteSpace(listing.Name));
                count += 1;
                if (count > MAX_RESULTS) return;
            }
            shop.Dispose();
        }

        [Fact]
        public async void Search_USD_ResultsFound()
        {
            //Given
            const int MAX_RESULTS = 120;
            Shop shop = new Shop();
            shop.UseProxy = false;
            shop.Initialize();
            //When
            shop.SetupSession("mpu6050", Currency.USD);
            //Then

            int count = 0;
            await foreach (ProductListing listing in shop)
            {
                Assert.False(string.IsNullOrWhiteSpace(listing.Name));
                Assert.True(listing.LowerPrice != 0);
                count += 1;
                if (count > MAX_RESULTS) return;
            }
            shop.Dispose();
        }
    }
}
