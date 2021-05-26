using System;
using MultiShop.Shared;
using MultiShop.Shared.Models;

namespace MultiShop.Client.Extensions
{
    public static class ResultProfileExtensions
    {
        public static int? CompareListings(this ResultsProfile.Category category, ProductListingInfo a, ProductListingInfo b)
        {
            switch (category)
            {
                case ResultsProfile.Category.RatingPriceRatio:
                    float? dealDiff = a.RatingToPriceRatio - b.RatingToPriceRatio;
                    if (!dealDiff.HasValue) return null;
                    int dealCeil = (int)Math.Ceiling(Math.Abs(dealDiff.Value));
                    return dealDiff < 0 ? -dealCeil : dealCeil;
                case ResultsProfile.Category.Price:
                    float priceDiff = b.Listing.UpperPrice - a.Listing.UpperPrice;
                    int priceCeil = (int)Math.Ceiling(Math.Abs(priceDiff));
                    return priceDiff < 0 ? -priceCeil : priceCeil;
                case ResultsProfile.Category.Purchases:
                    return a.Listing.PurchaseCount - b.Listing.PurchaseCount;
                case ResultsProfile.Category.Reviews:
                    return a.Listing.ReviewCount - b.Listing.ReviewCount;
            }

            throw new ArgumentException($"{category} does not have a defined comparison.");
        }

        public static string FriendlyName(this ResultsProfile.Category category)
        {
            switch (category)
            {
                case ResultsProfile.Category.RatingPriceRatio:
                    return "Best rating to price ratio first";
                case ResultsProfile.Category.Price:
                    return "Lowest price first";
                case ResultsProfile.Category.Purchases:
                    return "Most purchases first";
                case ResultsProfile.Category.Reviews:
                    return "Most reviews first";
            }
            throw new ArgumentException($"{category} does not have a friendly name defined.");
        }
    }
}