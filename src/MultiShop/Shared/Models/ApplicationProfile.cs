namespace MultiShop.Shared.Models
{
    public class ApplicationProfile
    {
        public int Id { get; set; }
        
        public string ApplicationUserId { get; set; }

        public bool DarkMode { get; set; }

        public bool CacheCommonSearches { get; set; } = true;

        public bool EnableSearchHistory { get; set; } = true;
    }
}