using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MultiShop.Client.Module;
using MultiShop.Shared.Models;
using MultiShop.Shop.Framework;

namespace MultiShop.Client.Shared
{
    public partial class CascadingDependencies : IDisposable
    {
        [Inject]
        private ILogger<CascadingDependencies> Logger { get; set; }

        [Inject]
        private IHttpClientFactory HttpClientFactory { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [Parameter]
        public RenderFragment<string> LoadingContent { get; set; }

        [Parameter]
        public RenderFragment Content { get; set; }

        private bool disposedValue;
        
        private string loadingDisplay;

        private IReadOnlyDictionary<string, IShop> shops;

        private ApplicationProfile applicationProfile;

        protected override async Task OnInitializedAsync()
        {
            loadingDisplay = "";
            await base.OnInitializedAsync();
            await DownloadShops(HttpClientFactory.CreateClient("Public-MultiShop.ServerAPI"));
            await DownloadApplicationProfile(HttpClientFactory.CreateClient("MultiShop.ServerAPI"));
            loadingDisplay = null;
        }

        private async Task DownloadShops(HttpClient http)
        {
            loadingDisplay = "shops";
            ShopModuleLoader loader = new ShopModuleLoader(http, Logger);
            shops = await loader.GetShops();
        }

        private async Task DownloadApplicationProfile(HttpClient http)
        {
            loadingDisplay = "profile";
            AuthenticationState authState = await AuthenticationStateTask;
            if (authState.User.Identity.IsAuthenticated)
            {
                Logger.LogDebug($"User is logged in. Attempting to fetch application profile.");
                HttpResponseMessage response = await http.GetAsync("Profile/Application");
                if (response.IsSuccessStatusCode)
                {
                    applicationProfile = await response.Content.ReadFromJsonAsync<ApplicationProfile>();
                }
            }
            if (applicationProfile == null) applicationProfile = new ApplicationProfile();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (string shopName in shops.Keys)
                    {
                        shops[shopName].Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}