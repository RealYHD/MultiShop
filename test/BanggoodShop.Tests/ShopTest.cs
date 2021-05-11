using MultiShop.ShopFramework;
using SimpleLogger;
using Xunit;
using Xunit.Abstractions;

namespace BanggoodShop.Tests
{
    public class ShopTest
    {
        public ShopTest(ITestOutputHelper output)
        {
            Logger.AddLogListener(new XUnitLogger(output));
        }


        [Fact]
        public async void Search_CAD_ResultsFound()
        {
            //Given
            const int MAX_RESULTS = 100;
            Shop shop = new Shop();
            shop.UseProxy = false;
            //When
            shop.Initialize();
            shop.SetupSession("samsung galaxy 20 case", Currency.CAD);
            //Then
            int count = 0;
            await foreach (ProductListing listing in shop)
            {
                count += 1;
                Assert.False(string.IsNullOrWhiteSpace(listing.Name));
                if (count >= MAX_RESULTS) return;
            }
        }
    }
}
