using System.Collections.Generic;
using MultiShop.ShopFramework;

namespace MultiShop.DataStructures
{
    public class SearchProfile
    {
        public Currency currency;
        public int maxResults;
        public float minRating;
        public bool keepUnrated;
        public bool enableUpperPrice;
        private int upperPrice;
        public int UpperPrice
        {
            get
            {
                return upperPrice;
            }
            set
            {
                if (enableUpperPrice) upperPrice = value;
            }
        }
        public int lowerPrice;
        public int minPurchases;
        public bool keepUnknownPurchaseCount;
        public int minReviews;
        public bool keepUnknownRatingCount;
        public bool enableMaxShippingFee;
        private int maxShippingFee;
        public int MaxShippingFee {
            get {
                return maxShippingFee;
            }
            set {
                if (enableMaxShippingFee) maxShippingFee = value;
            }
        }
        public bool keepUnknownShipping;
        public ShopStateTracker shopStates = new ShopStateTracker();

        public SearchProfile()
        {
            currency = Currency.CAD;
            maxResults = 100;
            minRating = 0.8f;
            keepUnrated = true;
            enableUpperPrice = false;
            upperPrice = 0;
            lowerPrice = 0;
            minPurchases = 0;
            keepUnknownPurchaseCount = true;
            minReviews = 0;
            keepUnknownRatingCount = true;
            enableMaxShippingFee = false;
            maxShippingFee = 0;
            keepUnknownShipping = true;
        }

        public class ShopStateTracker
        {
            private HashSet<string> shopsEnabled = new HashSet<string>();
            public bool this[string name]
            {
                get
                {
                    return shopsEnabled.Contains(name);
                }

                set
                {
                    if (value == false && !(shopsEnabled.Count > 1)) return;
                    if (value)
                    {
                        shopsEnabled.Add(name);
                    }
                    else
                    {
                        shopsEnabled.Remove(name);
                    }
                }
            }
            public bool IsToggleable(string shop) {
                return (shopsEnabled.Contains(shop) && shopsEnabled.Count > 1) || !shopsEnabled.Contains(shop);
            }
        }
    }
}