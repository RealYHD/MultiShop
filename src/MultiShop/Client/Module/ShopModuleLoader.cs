using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiShop.Shop.Framework;

namespace MultiShop.Client.Module
{
    public class ShopModuleLoader
    {
        private HttpClient http;
        private ILogger logger;
        private string[] moduleNames;
        private string[] dependencyNames;

        public ShopModuleLoader(HttpClient http, ILogger logger)
        {
            this.http = http;
            this.logger = logger;
        }

        private async Task DownloadAssembliesList() {
            moduleNames = await http.GetFromJsonAsync<string[]>("ShopModule/Modules");
            dependencyNames = await http.GetFromJsonAsync<string[]>("ShopModule/Dependencies");
        }

        private async Task<IReadOnlyDictionary<string, byte[]>> DownloadShopModuleAssemblies() {
            Dictionary<string, byte[]> assemblyData = new Dictionary<string, byte[]>();

            Dictionary<Task<byte[]>, string> downloadTasks = new Dictionary<Task<byte[]>, string>();

            logger.LogInformation("Beginning to download shop modules...");
            foreach (string moduleName in moduleNames)
            {
                logger.LogDebug($"Downloading shop: {moduleName}");
                downloadTasks.Add(http.GetByteArrayAsync("shopModule/Modules/" + moduleName), moduleName);
            }
            logger.LogInformation("Beginning to download shop module dependencies...");
            foreach (string depName in dependencyNames)
            {
                logger.LogDebug($"Downloading shop module dependency: {depName}");
                downloadTasks.Add(http.GetByteArrayAsync("ShopModule/Dependencies/" + depName), depName);
            }

            while (downloadTasks.Count > 0)
            {
                Task<byte[]> downloadTask = await Task.WhenAny(downloadTasks.Keys);
                assemblyData.Add(downloadTasks[downloadTask], await downloadTask);
                logger.LogDebug($"Shop module \"{downloadTasks[downloadTask]}\" completed downloading.");
                downloadTasks.Remove(downloadTask);
            }
            logger.LogInformation($"Downloaded {assemblyData.Count} assemblies in total.");
            return assemblyData;
        }

        public async Task<IReadOnlyDictionary<string, IShop>> GetShops()
        {
            await DownloadAssembliesList();

            Dictionary<string, IShop> shops = new Dictionary<string, IShop>();

            IReadOnlyDictionary<string, byte[]> assemblyData = await DownloadShopModuleAssemblies();

            ShopModuleLoadContext context = new ShopModuleLoadContext(assemblyData);
            logger.LogInformation("Beginning to load shop modules.");
            foreach (string moduleName in moduleNames)
            {
                logger.LogDebug($"Attempting to load shop module: \"{moduleName}\"");
                Assembly moduleAssembly = context.LoadFromAssemblyName(new AssemblyName(moduleName));
                bool shopLoaded = false;
                foreach (Type type in moduleAssembly.GetTypes())
                {
                    if (typeof(IShop).IsAssignableFrom(type)) {
                        IShop shop = Activator.CreateInstance(type) as IShop;
                        if (shop != null) {
                            shopLoaded = true;
                            shop.Initialize();
                            shops.Add(shop.ShopName, shop);
                            logger.LogDebug($"Added shop: {shop.ShopName}");
                        }
                    }
                }
                if (!shopLoaded) {
                    logger.LogWarning($"Module \"{moduleName}\" was reported to be a shop module, but did not contain a shop interface. Please report this to the site administrator.");
                }
            }
            logger.LogInformation($"Shop module loading complete. Loaded a total of {shops.Count} shops.");
            foreach (string assemblyName in context.UseCounter.Keys)
            {
                int usage = context.UseCounter[assemblyName];
                logger.LogDebug($"\"{assemblyName}\" was used {usage} times.");
                if (usage <= 0) {
                    logger.LogWarning($"\"{assemblyName}\" was not used. Please report this to the site administrator.");
                }
            }
            
            return shops;
        }
    }
}