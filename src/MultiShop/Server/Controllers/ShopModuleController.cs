using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MultiShop.Server.Options;
using SimpleLogger;

namespace MultiShop.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ShopModuleController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private IDictionary<string, byte[]> shopModules;
        private IDictionary<string, byte[]> shopModuleDependencies;
        

        public ShopModuleController(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.shopModules = new Dictionary<string, byte[]>();
            this.shopModuleDependencies = new Dictionary<string, byte[]>();

            ShopModulesOptions options = configuration.GetSection(ShopModulesOptions.ShopModules).Get<ShopModulesOptions>();
            foreach (string file in Directory.EnumerateFiles(options.Directory))
            {
                try
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(file);
                    shopModules.Add(assemblyName.FullName, System.IO.File.ReadAllBytes(file));
                }
                catch (BadImageFormatException e) {
                    Logger.Log($"\"{e.FileName}\" is not a valid assembly. Ignoring.", LogLevel.Warning);
                }
                catch (ArgumentException) {
                    Logger.Log($"\"{Path.GetFileName(file)}\" has the same full name as another assembly. Ignoring this one.", LogLevel.Warning);
                }
            }

            foreach (string file in Directory.EnumerateFiles(Path.Join(options.Directory, "dependencies")))
            {
                try
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(file);
                    shopModuleDependencies.Add(assemblyName.FullName, System.IO.File.ReadAllBytes(file));
                }
                catch (BadImageFormatException e) {
                    Logger.Log($"\"{e.FileName}\" is not a valid assembly. Ignoring.", LogLevel.Warning);
                }
                catch (ArgumentException) {
                    Logger.Log($"\"{Path.GetFileName(file)}\" has the same full name as another assembly. Ignoring this one.", LogLevel.Warning);
                }
            }
        }
        
        [HttpGet]
        [Route("Modules")]
        public IActionResult GetShopModuleNames() {
            return Ok(shopModules.Keys);
        }

        [HttpGet]
        [Route("Modules/{shopModuleName}")]
        public IActionResult GetModule(string shopModuleName) {
            ShopModulesOptions options = configuration.GetSection(ShopModulesOptions.ShopModules).Get<ShopModulesOptions>();
            if (!shopModules.ContainsKey(shopModuleName)) return NotFound();
            if (options.Disabled != null && options.Disabled.Contains(shopModuleName)) return Forbid();
            return File(shopModules[shopModuleName], "application/module-dll");
        }

        [HttpGet]
        [Route("Dependencies")]
        public IActionResult GetDependencyNames() {
            return Ok(shopModuleDependencies.Keys);
        }

        [HttpGet]
        [Route("Dependencies/{dependencyName}")]
        public IActionResult GetDependency(string dependencyName) {
            ShopModulesOptions options = configuration.GetSection(ShopModulesOptions.ShopModules).Get<ShopModulesOptions>();
            if (!shopModuleDependencies.ContainsKey(dependencyName)) return NotFound();
            return File(shopModuleDependencies[dependencyName], "application/module-dep-dll");
        }
    }
}