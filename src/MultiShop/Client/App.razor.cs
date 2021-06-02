using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MultiShop.Client.Module;
using MultiShop.Client.Shared;
using MultiShop.Shared.Models;
using MultiShop.Shop.Framework;

namespace MultiShop.Client
{
    public partial class App
    {
        private ICollection<RuntimeDependencyManager.Dependency> dependencies = new List<RuntimeDependencyManager.Dependency>();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dependencies.Add(new RuntimeDependencyManager.Dependency(typeof(IReadOnlyDictionary<string, IShop>), "Shops", DownloadShops));
            dependencies.Add(new RuntimeDependencyManager.Dependency(typeof(ApplicationProfile), "Application Profile", DownloadApplicationProfile));
        }

        private async ValueTask<object> DownloadShops(HttpClient publicHttp, HttpClient http, AuthenticationState authState, ILogger logger)
        {
            ShopModuleLoader loader = new ShopModuleLoader(publicHttp, logger);
            return await loader.GetShops();
        }

        private async ValueTask<object> DownloadApplicationProfile(HttpClient publicHttp, HttpClient http, AuthenticationState authState, ILogger logger)
        {
            if (authState.User.Identity.IsAuthenticated)
            {
                logger.LogDebug($"User is logged in. Attempting to fetch application profile.");
                HttpResponseMessage response = await http.GetAsync("Profile/Application");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApplicationProfile>();
                }
            }
            return new ApplicationProfile();
        }
    }
}