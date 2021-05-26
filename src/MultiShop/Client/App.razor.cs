using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using MultiShop.Client.Module;
using MultiShop.Shop.Framework;
using SimpleLogger;

namespace MultiShop.Client
{
    public partial class App
    {
        private bool modulesLoaded = false;

        private Dictionary<string, IShop> shops = new Dictionary<string, IShop>();
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DownloadShopModules();
        }
        private async Task DownloadShopModules()
        {
            HttpClient http = HttpFactory.CreateClient("Public-MultiShop.ServerAPI");

            Dictionary<string, byte[]> assemblyData = new Dictionary<string, byte[]>();

            string[] moduleNames = await http.GetFromJsonAsync<string[]>("ShopModule/Modules");
            string[] dependencyNames = await http.GetFromJsonAsync<string[]>("ShopModule/Dependencies");
            Dictionary<Task<byte[]>, string> downloadTasks = new Dictionary<Task<byte[]>, string>();

            Logger.Log("Beginning to download shop modules...");
            foreach (string moduleName in moduleNames)
            {
                Logger.Log($"Downloading shop: {moduleName}", LogLevel.Debug);
                downloadTasks.Add(http.GetByteArrayAsync("shopModule/Modules/" + moduleName), moduleName);
            }
            Logger.Log("Beginning to download shop module dependencies...");
            foreach (string depName in dependencyNames)
            {
                Logger.Log($"Downloading shop module dependency: {depName}", LogLevel.Debug);
                downloadTasks.Add(http.GetByteArrayAsync("ShopModule/Dependencies/" + depName), depName);
            }

            while (downloadTasks.Count > 0)
            {
                Task<byte[]> downloadTask = await Task.WhenAny(downloadTasks.Keys);
                assemblyData.Add(downloadTasks[downloadTask], await downloadTask);
                Logger.Log($"Shop module \"{downloadTasks[downloadTask]}\" completed downloading.", LogLevel.Debug);
                downloadTasks.Remove(downloadTask);
            }
            Logger.Log($"Downloaded {assemblyData.Count} assemblies in total.");

            ShopModuleLoadContext context = new ShopModuleLoadContext(assemblyData);
            Logger.Log("Beginning to load shop modules.");
            foreach (string moduleName in moduleNames)
            {
                Logger.Log($"Attempting to load shop module: \"{moduleName}\"", LogLevel.Debug);
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
                            Logger.Log($"Added shop: {shop.ShopName}", LogLevel.Debug);
                        }
                    }
                }
                if (!shopLoaded) {
                    Logger.Log($"Module \"{moduleName}\" was reported to be a shop module, but did not contain a shop interface. Please report this to the site administrator.", LogLevel.Warning);
                }
            }
            Logger.Log($"Shop module loading complete. Loaded a total of {shops.Count} shops.");
            modulesLoaded = true;
            foreach (string assemblyName in context.UseCounter.Keys)
            {
                int usage = context.UseCounter[assemblyName];
                Logger.Log($"\"{assemblyName}\" was used {usage} times.", LogLevel.Debug);
                if (usage <= 0) {
                    Logger.Log($"\"{assemblyName}\" was not used at all.", LogLevel.Warning);
                }
            }
        }

        public void Dispose()
        {
            foreach (string name in shops.Keys)
            {
                shops[name].Dispose();
                Logger.Log($"Ending lifetime of shop module for \"{name}\".", LogLevel.Debug);
            }
        }
    }
}