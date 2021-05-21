namespace MultiShop.Shop.Framework
{
    public struct ProductListing
    {
        public float LowerPrice { get; set; }
        public float UpperPrice { get; set; }
        public float? Shipping { get; set; }
        public string Name { get; set; }
        public string URL { get; set; }
        public string ImageURL { get; set; }
        public float? Rating { get; set; }
        public int? PurchaseCount { get; set; }
        public int? ReviewCount { get; set; }
        public bool ConvertedPrices { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            ProductListing b = (ProductListing)obj;
            return this.URL == b.URL;
        }

        public override int GetHashCode()
        {
            return URL.GetHashCode();
        }
    }
}