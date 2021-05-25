using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using MultiShop.Client.Pages;
using MultiShop.Shared.Models;

namespace MultiShop.Client.Listing
{
    public abstract class ListingView : ComponentBase
    {
        public abstract Views View { get; }

        [Parameter]
        public IList<ProductListingInfo> Listings { get; set; }

        [Parameter]
        public Search.Status Status { get; set; }

        private protected abstract string GetCategoryTag(ResultsProfile.Category category);

        public ListingView(Search.Status status)
        {
            this.Status = status;
        }
    }
}