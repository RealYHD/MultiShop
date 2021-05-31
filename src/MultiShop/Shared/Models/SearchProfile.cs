using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MultiShop.Shop.Framework;

namespace MultiShop.Shared.Models
{
    public class SearchProfile
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }

        public Currency Currency { get; set; } = Currency.CAD;
        public int MaxResults { get; set; } = 100;
        public float MinRating { get; set; } = 0.8f;
        public bool KeepUnrated { get; set; } = true;
        public bool EnableUpperPrice { get; set; } = false;
        private int _upperPrice;

        public int UpperPrice
        {
            get
            {
                return _upperPrice;
            }
            set
            {
                if (EnableUpperPrice) _upperPrice = value;
            }
        }
        public int LowerPrice { get; set; }
        public int MinPurchases { get; set; }
        public bool KeepUnknownPurchaseCount { get; set; } = true;
        public int MinReviews { get; set; }
        public bool KeepUnknownRatingCount { get; set; } = true;
        public bool EnableMaxShippingFee { get; set; }
        private int _maxShippingFee;

        public int MaxShippingFee
        {
            get
            {
                return _maxShippingFee;
            }
            set
            {
                if (EnableMaxShippingFee) _maxShippingFee = value;
            }
        }
        public bool KeepUnknownShipping { get; set; } = true;

        [Required]
        public ShopToggler ShopStates { get; set; } = new ShopToggler();

        public sealed class ShopToggler : HashSet<string>
        {
            public int TotalShops { get; set; }
            public bool this[string name] {
                get {
                    return !this.Contains(name);
                }
                set {
                    if (value == false && TotalShops - Count <= 1) return;
                    if (value)
                    {
                        this.Remove(name);
                    }
                    else
                    {
                        this.Add(name);
                    }
                }
            }

            public ShopToggler Clone() {
                ShopToggler clone = new ShopToggler();
                clone.Union(this);
                return clone;
            }

            public bool IsShopToggleable(string shop)
            {
                return (!Contains(shop) && TotalShops - Count > 1) || Contains(shop);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            SearchProfile other = (SearchProfile) obj;
            return 
                Id == other.Id && 
                Currency == other.Currency && 
                MaxResults == other.MaxResults && 
                MinRating == other.MinRating && 
                KeepUnrated == other.KeepUnrated && 
                EnableUpperPrice == other.EnableUpperPrice && 
                UpperPrice == other.UpperPrice && 
                LowerPrice == other.LowerPrice && 
                MinPurchases == other.MinPurchases && 
                KeepUnknownPurchaseCount == other.KeepUnknownPurchaseCount && 
                MinReviews == other.MinReviews && 
                KeepUnknownRatingCount == other.KeepUnknownRatingCount && 
                EnableMaxShippingFee == other.EnableMaxShippingFee && 
                MaxShippingFee == other.MaxShippingFee && 
                KeepUnknownShipping == other.KeepUnknownShipping && 
                ShopStates.Equals(other.ShopStates);
        }
        
        public override int GetHashCode()
        {
            return Id;
        }

        public SearchProfile DeepCopy() {
            SearchProfile profile = (SearchProfile)MemberwiseClone();
            profile.ShopStates = ShopStates.Clone();
            return profile;
        }
    }
}