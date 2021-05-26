using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using MultiShop.Shop.Framework;

namespace MultiShop.Client.Pages
{
    public partial class Info
    {
        [CascadingParameter(Name = "Shops")]
        public Dictionary<string, IShop> Shops { get; set; }
    }
}