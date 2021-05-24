using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using MultiShop.Shop.Framework;
using SimpleLogger;

namespace MultiShop.Client
{
    public partial class App
    {
        private bool modulesLoaded = false;

        private Dictionary<string, IShop> shops = new Dictionary<string, IShop>();
        private Dictionary<string, byte[]> assemblyData = new Dictionary<string, byte[]>();
        private Dictionary<string, Assembly> assemblyCache = new Dictionary<string, Assembly>();
        protected override async Task OnInitializedAsync()
        {
            await DownloadShopModules();
            await base.OnInitializedAsync();
        }
        private async Task DownloadShopModules()
        {
            HttpClient http = HttpFactory.CreateClient("Public-MultiShop.ServerAPI");
            Logger.Log($"Fetching shop modules.", LogLevel.Debug);
            string[] assemblyFileNames = await http.GetFromJsonAsync<string[]>("ShopModules");
            Dictionary<Task<byte[]>, string> downloadTasks = new Dictionary<Task<byte[]>, string>(assemblyFileNames.Length);

            foreach (string assemblyFileName in assemblyFileNames)
            {
                Logger.Log($"Downloading \"{assemblyFileName}\"...", LogLevel.Debug);
                downloadTasks.Add(http.GetByteArrayAsync(Path.Join("ShopModules", assemblyFileName)), assemblyFileName);
            }

            while (downloadTasks.Count != 0)
            {
                Task<byte[]> data = await Task.WhenAny(downloadTasks.Keys);
                string assemblyFileName = downloadTasks[data];
                Logger.Log($"\"{assemblyFileName}\" completed downloading.", LogLevel.Debug);
                assemblyData.Add(assemblyFileName, data.Result);
                downloadTasks.Remove(data);
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyDependencyRequest;

            foreach (string assemblyFileName in assemblyData.Keys)
            {
                Assembly assembly = AppDomain.CurrentDomain.Load(assemblyData[assemblyFileName]);
                bool used = false;
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IShop).IsAssignableFrom(type))
                    {
                        IShop shop = Activator.CreateInstance(type) as IShop;
                        if (shop != null)
                        {
                            shop.Initialize();
                            shops.Add(shop.ShopName, shop);
                            Logger.Log($"Registered and started lifetime of module for \"{shop.ShopName}\".", LogLevel.Debug);
                            used = true;
                        }
                    }
                }
                if (!used) {
                    Logger.Log($"Since unused, caching \"{assemblyFileName}\".", LogLevel.Debug);
                    assemblyCache.Add(assemblyFileName, assembly);
                }
                assemblyData.Remove(assemblyFileName);
            }
            foreach (string assembly in assemblyData.Keys)
            {
                Logger.Log($"\"{assembly}\" was unused.", LogLevel.Warning);
            }
            foreach (string assembly in assemblyCache.Keys)
            {
                Logger.Log($"\"{assembly}\" was unused.", LogLevel.Warning);
            }
            assemblyData.Clear();
            assemblyCache.Clear();
            modulesLoaded = true;
        }


        private Assembly OnAssemblyDependencyRequest(object sender, ResolveEventArgs args)
        {
            string dependencyName = args.Name.Substring(0, args.Name.IndexOf(','));
            Logger.Log($"Assembly \"{args.RequestingAssembly.GetName().Name}\" is requesting dependency assembly \"{dependencyName}\".", LogLevel.Debug);
            if (assemblyCache.ContainsKey(dependencyName)) {
                Logger.Log($"Found \"{dependencyName}\" in cache.", LogLevel.Debug);
                Assembly dep = assemblyCache[dependencyName];
                assemblyCache.Remove(dependencyName);
                return dep;
            } else if (assemblyData.ContainsKey(dependencyName)) {
                return AppDomain.CurrentDomain.Load(assemblyData[dependencyName]);
            } else {
                Logger.Log($"No dependency under name \"{args.Name}\"", LogLevel.Warning);
                return null;
            }
        }


        public void Dispose()
        {
            foreach (string name in shops.Keys)
            {
                shops[name].Dispose();
                Logger.Log($"Ending lifetime of shop module for \"{name}\".");
            }
        }
    }
}