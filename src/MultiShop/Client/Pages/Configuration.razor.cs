using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MultiShop.Client.Services;
using MultiShop.Shared.Models;

namespace MultiShop.Client.Pages
{
    public partial class Configuration : IAsyncDisposable
    {
        [Inject]
        private ILogger<Configuration> Logger { get; set; }

        [Inject]
        private LayoutStateChangeNotifier LayoutStateChangeNotifier { get; set; }
        
        [Inject]
        private HttpClient Http { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [CascadingParameter(Name = "RuntimeDependencyManager")]
        private RuntimeDependencyManager RuntimeDependencyManager { get; set; }

        private ApplicationProfile ApplicationProfile { get; set; }

        private bool collapseNavMenu = true;
        private string NavMenuCssClass => (collapseNavMenu ? "collapse" : "");

        private enum Section
        {
            Opening, UI, Search
        }

        private Section currentSection = Section.Opening;

        private Dictionary<Section, string> sectionNames = new Dictionary<Section, string>() {
            {Section.Opening, "Info"},
            {Section.UI, "UI"},
            {Section.Search, "Search"}
        };

        private List<Section> sectionOrder = new List<Section>() {
            Section.Opening,
            Section.UI,
            Section.Search
        };

        protected override void OnInitialized()
        {
            base.OnInitialized();
            ApplicationProfile = RuntimeDependencyManager.Get<ApplicationProfile>();
        }

        private string GetNavItemCssClass(Section section)
        {
            return "nav-item" + ((section == currentSection) ? " active" : null);
        }

        public async ValueTask DisposeAsync()
        {
            AuthenticationState authenticationState = await AuthenticationStateTask;
            if (authenticationState.User.Identity.IsAuthenticated) {
                Logger.LogDebug($"User is authenticated. Attempting to save configuration to server.");
                await Http.PutAsJsonAsync("Profile/Application", ApplicationProfile);
            }
        }
    }
}