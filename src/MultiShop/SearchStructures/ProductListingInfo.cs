using System.Collections.Generic;
using MultiShop.ShopFramework;

namespace MultiShop.SearchStructures
{
    public class ProductListingInfo
    {
        public ProductListing Listing { get; private set; }
        public string ShopName { get; private set; }
        public float RatingToPriceRatio {
            get {
                int reviewFactor = Listing.ReviewCount.HasValue ? Listing.ReviewCount.Value : 1;
                int purchaseFactor = Listing.PurchaseCount.HasValue ? Listing.PurchaseCount.Value : 1;
                float ratingFactor = 1 + (Listing.Rating.HasValue ? Listing.Rating.Value : 0);
                return (ratingFactor * (reviewFactor > purchaseFactor ? reviewFactor : purchaseFactor))/(Listing.LowerPrice * Listing.UpperPrice);
            }
        }
        public ISet<ResultsProfile.Category> Tops { get; private set; } = new HashSet<ResultsProfile.Category>();

        public ProductListingInfo(ProductListing listing, string shopName)
        {
            this.Listing = listing;
            this.ShopName = shopName;
        }
    }
}