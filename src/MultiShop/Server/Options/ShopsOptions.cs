using System.Collections.Generic;

namespace MultiShop.Server.Options
{
    //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#bind-hierarchical-configuration-data-using-the-options-pattern
    public class ShopOptions
    {
        public const string Shop = "Shops";
        public string Directory { get; set; }
        public IList<string> Disabled { get; set; }
    }

}