using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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
        
        public IEnumerable<string> GetShopModuleNames() {
            foreach (string file in Directory.EnumerateFiles(configuration["ModulesDir"]))
            {
                if (Path.GetExtension(file).ToLower().Equals(".dll")) {
                    yield return Path.GetFileNameWithoutExtension(file);
                }
            }
        }


        [HttpGet]
        [Route("{shopModuleName}")]
        public ActionResult GetModule(string shopModuleName) {
            string shopPath = Path.Join(configuration["ModulesDir"], shopModuleName);
            if (!System.IO.File.Exists(shopPath)) return NotFound();
            return File(new FileStream(shopPath, FileMode.Open), "application/shop-dll");
        }
    }
}