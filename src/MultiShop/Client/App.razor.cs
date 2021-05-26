using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MultiShop.Client.Module;
using MultiShop.Shop.Framework;

namespace MultiShop.Client
{
    public partial class App : IDisposable
    {
        [Inject]
        private IHttpClientFactory HttpFactory { get; set; }
        
        [Inject]
        private ILogger<App> Logger {get; set; }

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

            Logger.LogInformation("Beginning to download shop modules...");
            foreach (string moduleName in moduleNames)
            {
                Logger.LogDebug($"Downloading shop: {moduleName}");
                downloadTasks.Add(http.GetByteArrayAsync("shopModule/Modules/" + moduleName), moduleName);
            }
            Logger.LogInformation("Beginning to download shop module dependencies...");
            foreach (string depName in dependencyNames)
            {
                Logger.LogDebug($"Downloading shop module dependency: {depName}");
                downloadTasks.Add(http.GetByteArrayAsync("ShopModule/Dependencies/" + depName), depName);
            }

            while (downloadTasks.Count > 0)
            {
                Task<byte[]> downloadTask = await Task.WhenAny(downloadTasks.Keys);
                assemblyData.Add(downloadTasks[downloadTask], await downloadTask);
                Logger.LogDebug($"Shop module \"{downloadTasks[downloadTask]}\" completed downloading.");
                downloadTasks.Remove(downloadTask);
            }
            Logger.LogInformation($"Downloaded {assemblyData.Count} assemblies in total.");

            ShopModuleLoadContext context = new ShopModuleLoadContext(assemblyData);
            Logger.LogInformation("Beginning to load shop modules.");
            foreach (string moduleName in moduleNames)
            {
                Logger.LogDebug($"Attempting to load shop module: \"{moduleName}\"");
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
                            Logger.LogDebug($"Added shop: {shop.ShopName}");
                        }
                    }
                }
                if (!shopLoaded) {
                    Logger.LogWarning($"Module \"{moduleName}\" was reported to be a shop module, but did not contain a shop interface. Please report this to the site administrator.");
                }
            }
            Logger.LogInformation($"Shop module loading complete. Loaded a total of {shops.Count} shops.");
            modulesLoaded = true;
            foreach (string assemblyName in context.UseCounter.Keys)
            {
                int usage = context.UseCounter[assemblyName];
                Logger.LogDebug($"\"{assemblyName}\" was used {usage} times.");
                if (usage <= 0) {
                    Logger.LogWarning($"\"{assemblyName}\" was not used. Please report this to the site administrator.");
                }
            }
        }

        public void Dispose()
        {
            foreach (string name in shops.Keys)
            {
                shops[name].Dispose();
            }
        }
    }
}