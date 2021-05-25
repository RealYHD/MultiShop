using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MultiShop.Server.Options;

namespace MultiShop.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShopModulesController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDictionary<string, byte[]> shopAssemblyData;

        

        public ShopModulesController(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.shopAssemblyData = new Dictionary<string, byte[]>();
        }
        
        public IActionResult GetShopModuleNames() {
            List<string> moduleNames = new List<string>();
            ShopOptions options = configuration.GetSection(ShopOptions.Shop).Get<ShopOptions>();
            foreach (string file in Directory.EnumerateFiles(options.Directory))
            {
                if (Path.GetExtension(file).ToLower().Equals(".dll") && !(options.Disabled != null && options.Disabled.Contains(Path.GetFileNameWithoutExtension(file)))) {
                    moduleNames.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            return Ok(moduleNames);
        }


        [HttpGet]
        [Route("{shopModuleName}")]
        public IActionResult GetModule(string shopModuleName) {
            ShopOptions options = configuration.GetSection(ShopOptions.Shop).Get<ShopOptions>();
            string shopPath = Path.Join(options.Directory, shopModuleName);
            shopPath += ".dll";
            if (!System.IO.File.Exists(shopPath)) return NotFound();
            if (options.Disabled != null && options.Disabled.Contains(shopModuleName)) return Forbid();
            return File(new FileStream(shopPath, FileMode.Open), "application/shop-dll");
        }
    }
}