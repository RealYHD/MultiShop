using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MultiShop.Shared;
using MultiShop.Shop.Framework;
using SimpleLogger;

namespace MultiShop.Client.Pages
{
    public partial class Search
    {
        [CascadingParameter(Name = "Shops")]
        public Dictionary<string, IShop> Shops { get; set; }

        [Parameter]
        public string Query { get; set; }

        private SearchProfile activeProfile = new SearchProfile();
        private ResultsProfile activeResultsProfile = new ResultsProfile();

        private bool showSearchConfiguration = false;
        private bool showResultsConfiguration = false;

        private string ToggleSearchConfigButtonCss
        {
            get => "btn btn-outline-secondary" + (showSearchConfiguration ? " active" : "");
        }

        private string ToggleResultsConfigurationcss {
            get => "btn btn-outline-secondary btn-tab" + (showResultsConfiguration ? " active" : "");
        }

        private bool searched = false;
        private bool searching = false;
        private bool organizing = false;

        private int resultsChecked = 0;
        private List<ProductListingInfo> listings = new List<ProductListingInfo>();

        protected override void OnInitialized()
        {
            foreach (string shop in Shops.Keys)
            {
                activeProfile.shopStates[shop] = true;
            }
            base.OnInitialized();
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(Query))
            {
                await PerformSearch(Query);
            }
            await base.OnParametersSetAsync();
        }

        private async Task PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            if (searching) return;
            searching = true;
            Logger.Log($"Received search request for \"{query}\".", LogLevel.Debug);
            resultsChecked = 0;
            listings.Clear();
            Dictionary<ResultsProfile.Category, List<ProductListingInfo>> greatest = new Dictionary<ResultsProfile.Category,
            List<ProductListingInfo>>();
            foreach (string shopName in Shops.Keys)
            {
                if (activeProfile.shopStates[shopName])
                {
                    Logger.Log($"Querying \"{shopName}\" for products.");
                    Shops[shopName].SetupSession(query, activeProfile.currency);
                    int shopViableResults = 0;
                    await foreach (ProductListing listing in Shops[shopName])
                    {
                        resultsChecked += 1;
                        if (resultsChecked % 50 == 0)
                        {
                            StateHasChanged();
                            await Task.Yield();
                        }


                        if (listing.Shipping == null && !activeProfile.keepUnknownShipping || (activeProfile.enableMaxShippingFee && listing.Shipping > activeProfile.MaxShippingFee)) continue;
                        float shippingDifference = listing.Shipping != null ? listing.Shipping.Value : 0;
                        if (!(listing.LowerPrice + shippingDifference >= activeProfile.lowerPrice && (!activeProfile.enableUpperPrice || listing.UpperPrice + shippingDifference <= activeProfile.UpperPrice))) continue;
                        if ((listing.Rating == null && !activeProfile.keepUnrated) && activeProfile.minRating > (listing.Rating == null ? 0 : listing.Rating)) continue;
                        if ((listing.PurchaseCount == null && !activeProfile.keepUnknownPurchaseCount) || activeProfile.minPurchases > (listing.PurchaseCount == null ? 0 : listing.PurchaseCount)) continue;
                        if ((listing.ReviewCount == null && !activeProfile.keepUnknownRatingCount) || activeProfile.minReviews > (listing.ReviewCount == null ? 0 : listing.ReviewCount)) continue;

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
                        if (shopViableResults >= activeProfile.maxResults) break;
                    }
                    Logger.Log($"\"{shopName}\" has completed. There are {listings.Count} results in total.", LogLevel.Debug);
                }
                else
                {
                    Logger.Log($"Skipping {shopName} since it's disabled.");
                }
            }
            searching = false;
            searched = true;

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

        private async Task Organize(List<ResultsProfile.Category> order)
        {
            if (searching) return;
            organizing = true;
            StateHasChanged();
            
            List<ProductListingInfo> sortedResults = await Task.Run<List<ProductListingInfo>>(() =>
            {
                List<ProductListingInfo> sorted = new List<ProductListingInfo>(listings);
                sorted.Sort((a, b) =>
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
                });
                return sorted;
            });
            listings.Clear();
            listings.AddRange(sortedResults);
            organizing = false;
            StateHasChanged();
        }


        private string GetOrNA(object data, string prepend = null, string append = null)
        {
            return data != null ? (prepend + data.ToString() + append) : "N/A";
        }

        private string CategoryTags(ResultsProfile.Category c)
        {
            switch (c)
            {
                case ResultsProfile.Category.RatingPriceRatio:
                    return "Best rating to price ratio";
                case ResultsProfile.Category.Price:
                    return "Lowest price";
                case ResultsProfile.Category.Purchases:
                    return "Most purchases";
                case ResultsProfile.Category.Reviews:
                    return "Most reviews";
            }
            throw new ArgumentException($"{c} does not have an associated string.");
        }
    }
}