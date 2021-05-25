using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using MultiShop.Shared;
using MultiShop.Shop.Framework;

namespace MultiShop.Shared.Models
{
    public class ProductListingInfo
    {
        public int Id { get; set; }

        private ProductListing? cachedListing;

        [Required]
        private string _listing = null;
        public ProductListing Listing
        {
            get
            {
                if (cachedListing == null) cachedListing = JsonSerializer.Deserialize<ProductListing>(_listing);
                return cachedListing.Value;
            }

            set
            {
                _listing = JsonSerializer.Serialize(value);
                cachedListing = value;
            }
        }
        public string ShopName { get; private set; }

        public float? RatingToPriceRatio
        {
            get
            {
                int reviewFactor = Listing.ReviewCount.HasValue ? Listing.ReviewCount.Value : 1;
                int purchaseFactor = Listing.PurchaseCount.HasValue ? Listing.PurchaseCount.Value : 1;
                return (Listing.Rating * (reviewFactor > purchaseFactor ? reviewFactor : purchaseFactor)) / (Listing.LowerPrice * Listing.UpperPrice);
            }
        }
        public ISet<ResultsProfile.Category> Tops { get; private set; } = new HashSet<ResultsProfile.Category>();

        public ProductListingInfo(ProductListing listing, string shopName)
        {
            this.Listing = listing;
            this.ShopName = shopName;
        }

        public ProductListingInfo()
        {

        }

        public override bool Equals(object obj)
        {

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            ProductListingInfo other = (ProductListingInfo)obj;
            return Id == other.Id && ShopName.Equals(other.ShopName) && Listing.Equals(other.Listing);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}