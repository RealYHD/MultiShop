using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MultiShop.Client.Module;
using MultiShop.Shared.Models;
using MultiShop.Shop.Framework;

namespace MultiShop.Client
{
    public partial class App
    {
        [Inject]
        private IJSRuntime JS { get; set; }
        [Inject]
        private IHttpClientFactory HttpClientFactory {get; set;}
        private IJSObjectReference localStorageManager;
        private ICollection<RuntimeDependencyManager.Dependency> dependencies = new List<RuntimeDependencyManager.Dependency>();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dependencies.Add(new RuntimeDependencyManager.Dependency(typeof(IJSObjectReference), "JS Modules", (pHttp, http, auth, logger) => new ValueTask<object>(localStorageManager), "LocalStorageManager"));
            dependencies.Add(new RuntimeDependencyManager.Dependency(typeof(IJSObjectReference), "JS Modules", async (pHttp, http, auth, logger) => await JS.InvokeAsync<IJSObjectReference>("import", "./js/Components/ComponentSupport.js"), "ComponentSupport"));
            dependencies.Add(new RuntimeDependencyManager.Dependency(typeof(IReadOnlyDictionary<string, IShop>), "Shops", async (publicHttp, authenticatedHttp, auth, logger) => await (new ShopModuleLoader(publicHttp, logger)).GetShops()));
            dependencies.Add(new RuntimeDependencyManager.Dependency(typeof(ApplicationProfile), "Application Profile", DownloadApplicationProfile));
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            localStorageManager = await JS.InvokeAsync<IJSObjectReference>("import", "./js/LocalStorageManager.js");
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
            ApplicationProfile profile = await localStorageManager.InvokeAsync<ApplicationProfile>("retrieve", "ApplicationProfile");
            if (profile != null) return profile;
            return new ApplicationProfile();
        }

    }
}