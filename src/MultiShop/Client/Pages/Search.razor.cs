using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MultiShop.Client.Extensions;
using MultiShop.Client.Listing;
using MultiShop.Client.Services;
using MultiShop.Client.Shared;
using MultiShop.Shared.Models;
using MultiShop.Shop.Framework;

namespace MultiShop.Client.Pages
{
    public partial class Search : IAsyncDisposable
    {
        [Inject]
        private ILogger<Search> Logger { get; set; }

        [Inject]
        private HttpClient Http { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState> AuthenticationStateTask { get; set; }

        [CascadingParameter(Name = "RuntimeDependencyManager")]
        public RuntimeDependencyManager RuntimeDependencyManager { get; set; }

        private IReadOnlyDictionary<string, IShop> Shops { get; set; }


        [Parameter]
        public string Query { get; set; }

        private SearchBar searchBar;

        private Status status = new Status();

        private Views CurrentView = Views.Table;

        private SearchProfile activeSearchProfile;
        private ResultsProfile activeResultsProfile;

        private List<ProductListingInfo> listings = new List<ProductListingInfo>();
        private int resultsChecked = 0;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Shops = RuntimeDependencyManager.Get<IReadOnlyDictionary<string, IShop>>();
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            AuthenticationState authState = await AuthenticationStateTask;

            if (authState.User.Identity.IsAuthenticated)
            {
                Logger.LogDebug($"User \"{authState.User.Identity.Name}\" is authenticated. Checking for saved profiles.");
                HttpResponseMessage searchProfileResponse = await Http.GetAsync("Profile/Search");
                if (searchProfileResponse.IsSuccessStatusCode)
                {
                    activeSearchProfile = await searchProfileResponse.Content.ReadFromJsonAsync<SearchProfile>();
                }

                HttpResponseMessage resultsProfileResponse = await Http.GetAsync("Profile/Results");
                if (resultsProfileResponse.IsSuccessStatusCode)
                {
                    activeResultsProfile = await resultsProfileResponse.Content.ReadFromJsonAsync<ResultsProfile>();
                }
            }
            IJSObjectReference localStorageManager = RuntimeDependencyManager.Get<IJSObjectReference>("LocalStorageManager");
            if (activeSearchProfile == null) activeSearchProfile = await localStorageManager.InvokeAsync<SearchProfile>("retrieve", "SearchProfile");
            if (activeResultsProfile == null) activeResultsProfile = await localStorageManager.InvokeAsync<ResultsProfile>("retrieve", "ResultsProfile");

            if (activeSearchProfile == null) activeSearchProfile = new SearchProfile();
            if (activeResultsProfile == null) activeResultsProfile = new ResultsProfile();
            activeSearchProfile.ShopStates.TotalShops = Shops.Count;

            if (Query != null)
            {
                searchBar.Searching = true;
                await PerformSearch(Query);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                searchBar.Query = Query;
            }
        }

        private async Task PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            if (status.Searching) return;
            searchBar.Searching = true;
            SearchProfile searchProfile = activeSearchProfile.DeepCopy();
            status.Searching = true;
            Logger.LogDebug($"Received search request for \"{query}\".");
            resultsChecked = 0;
            listings.Clear();
            Dictionary<ResultsProfile.Category, List<ProductListingInfo>> greatest = new Dictionary<ResultsProfile.Category,
            List<ProductListingInfo>>();
            foreach (string shopName in Shops.Keys)
            {
                if (searchProfile.ShopStates[shopName])
                {
                    Logger.LogDebug($"Querying \"{shopName}\" for products.");
                    Shops[shopName].SetupSession(query, searchProfile.Currency);
                    int shopViableResults = 0;
                    await foreach (ProductListing listing in Shops[shopName])
                    {
                        resultsChecked += 1;
                        if (resultsChecked % 50 == 0)
                        {
                            StateHasChanged();
                            await Task.Yield();
                        }


                        if (listing.Shipping == null && !searchProfile.KeepUnknownShipping || (searchProfile.EnableMaxShippingFee && listing.Shipping > searchProfile.MaxShippingFee)) continue;
                        float shippingDifference = listing.Shipping != null ? listing.Shipping.Value : 0;
                        if (!(listing.LowerPrice + shippingDifference >= searchProfile.LowerPrice && (!searchProfile.EnableUpperPrice || listing.UpperPrice + shippingDifference <= searchProfile.UpperPrice))) continue;
                        if ((listing.Rating == null && !searchProfile.KeepUnrated) && searchProfile.MinRating > (listing.Rating == null ? 0 : listing.Rating)) continue;
                        if ((listing.PurchaseCount == null && !searchProfile.KeepUnknownPurchaseCount) || searchProfile.MinPurchases > (listing.PurchaseCount == null ? 0 : listing.PurchaseCount)) continue;
                        if ((listing.ReviewCount == null && !searchProfile.KeepUnknownRatingCount) || searchProfile.MinReviews > (listing.ReviewCount == null ? 0 : listing.ReviewCount)) continue;

                        ProductListingInfo info = new ProductListingInfo(listing, shopName);
                        listings.Add(info);
                        foreach (ResultsProfile.Category c in Enum.GetValues<ResultsProfile.Category>())
                        {
                            if (!greatest.ContainsKey(c)) greatest[c] = new List<ProductListingInfo>();
                            if (greatest[c].Count > 0)
                            {
                                int? compResult = c.CompareListings(info, greatest[c][0]);
                                if (compResult.HasValue)
                                {
                                    if (compResult > 0) greatest[c].Clear();
                                    if (compResult >= 0) greatest[c].Add(info);
                                }
                            }
                            else
                            {
                                if (c.CompareListings(info, info).HasValue)
                                {
                                    greatest[c].Add(info);
                                }
                            }
                        }

                        shopViableResults += 1;
                        if (shopViableResults >= searchProfile.MaxResults) break;
                    }
                    Logger.LogDebug($"\"{shopName}\" has completed. There are {listings.Count} results in total.");
                }
                else
                {
                    Logger.LogDebug($"Skipping {shopName} since it's disabled.");
                }
            }
            status.Searching = false;
            status.Searched = true;
            searchBar.Searching = false;

            int tagsAdded = 0;
            foreach (ResultsProfile.Category c in greatest.Keys)
            {
                foreach (ProductListingInfo info in greatest[c])
                {
                    info.Tops.Add(c);
                    tagsAdded += 1;
                    if (tagsAdded % 50 == 0) await Task.Yield();
                }
            }

            await Organize(activeResultsProfile.Order);
        }

        private async Task Organize(IList<ResultsProfile.Category> order)
        {
            if (status.Searching || listings.Count <= 1) return;
            status.Organizing = true;
            Comparison<ProductListingInfo> comparer = (a, b) =>
                {
                    foreach (ResultsProfile.Category category in activeResultsProfile.Order)
                    {
                        int? compareResult = category.CompareListings(a, b);
                        if (compareResult.HasValue && compareResult.Value != 0)
                        {
                            return -compareResult.Value;
                        }
                    }
                    return 0;
                };

            Func<(int, int), Task<int>> partition = async (ilh) =>
            {
                ProductListingInfo swapTemp;
                ProductListingInfo pivot = listings[ilh.Item2];
                int lastSwap = ilh.Item1 - 1;
                for (int j = ilh.Item1; j <= ilh.Item2 - 1; j++)
                {
                    if (comparer.Invoke(listings[j], pivot) <= 0)
                    {
                        lastSwap += 1;
                        swapTemp = listings[lastSwap];
                        listings[lastSwap] = listings[j];
                        listings[j] = swapTemp;
                    }
                    await Task.Yield();
                }
                swapTemp = listings[lastSwap + 1];
                listings[lastSwap + 1] = listings[ilh.Item2];
                listings[ilh.Item2] = swapTemp;
                return lastSwap + 1;
            };

            Func<(int, int), Task> quickSort = async (ilh) =>
            {
                Stack<(int, int)> iterativeStack = new Stack<(int, int)>();
                iterativeStack.Push(ilh);

                while (iterativeStack.Count > 0)
                {
                    (int, int) lh = iterativeStack.Pop();
                    int p = await partition.Invoke((lh.Item1, lh.Item2));

                    if (p - 1 > lh.Item1)
                    {
                        iterativeStack.Push((lh.Item1, p - 1));
                    }

                    if (p + 1 < lh.Item2)
                    {
                        iterativeStack.Push((p + 1, lh.Item2));
                    }

                    await Task.Yield();
                }
            };
            StateHasChanged();

            await quickSort((0, listings.Count - 1));

            status.Organizing = false;
            StateHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            AuthenticationState authState = await AuthenticationStateTask;
            if (authState.User.Identity.IsAuthenticated)
            {
                await Http.PutAsJsonAsync("Profile/Search", activeSearchProfile);
                await Http.PutAsJsonAsync("Profile/Results", activeResultsProfile);
            }
            IJSObjectReference localStorageManager = RuntimeDependencyManager.Get<IJSObjectReference>("LocalStorageManager");
            await localStorageManager.InvokeVoidAsync("save", "SearchProfile", activeSearchProfile);
            await localStorageManager.InvokeVoidAsync("save", "ResultsProfile", activeResultsProfile);
        }

        public class Status
        {
            public bool SearchConfiguring { get; set; }
            public bool ResultsConfiguring { get; set; }
            public bool Organizing { get; set; }
            public bool Searching { get; set; }
            public bool Searched { get; set; }
        }
    }
}