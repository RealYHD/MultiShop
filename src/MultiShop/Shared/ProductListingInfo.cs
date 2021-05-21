using System.Collections.Generic;
using MultiShop.Shop.Framework;

namespace MultiShop.Shared
{
    public class ProductListingInfo
    {
        public ProductListing Listing { get; private set; }
        public string ShopName { get; private set; }
        public float? RatingToPriceRatio {
            get {
                int reviewFactor = Listing.ReviewCount.HasValue ? Listing.ReviewCount.Value : 1;
                int purchaseFactor = Listing.PurchaseCount.HasValue ? Listing.PurchaseCount.Value : 1;
                return (Listing.Rating * (reviewFactor > purchaseFactor ? reviewFactor : purchaseFactor))/(Listing.LowerPrice * Listing.UpperPrice);
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