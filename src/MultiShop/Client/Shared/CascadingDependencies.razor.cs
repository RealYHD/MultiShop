using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MultiShop.Shared.Models;
using MultiShop.Shop.Framework;

namespace MultiShop.Client.Shared
{
    public partial class CascadingDependencies : IAsyncDisposable
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

        [Parameter]
        public ICollection<RuntimeDependencyManager.Dependency> Dependencies { get; set; }
        
        private RuntimeDependencyManager manager;
        private bool disposedValue;
        
        private string loadingDisplay;

        protected override async Task OnInitializedAsync()
        {
            loadingDisplay = "stuff";
            await base.OnInitializedAsync();
            manager = new RuntimeDependencyManager(HttpClientFactory.CreateClient("Public-MultiShop.ServerAPI"), HttpClientFactory.CreateClient("MultiShop.ServerAPI"), AuthenticationStateTask, Logger);
            foreach (RuntimeDependencyManager.Dependency dep in Dependencies)
            {
                loadingDisplay = dep.DisplayName;
                await manager.SetupDependency(dep);
            }
            loadingDisplay = null;
        }

        

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue) {
                await manager.DisposeAsync();
            }
            disposedValue = true;
        }

    }
}