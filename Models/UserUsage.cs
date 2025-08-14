namespace NetRoll.Models
{
    public class UserUsage
    {
        public string OwnerUserId { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public long StorageBytes { get; set; }
        public int ProductCount { get; set; } // jövőbeni használat
        public DateTime UpdatedUtc { get; set; }
    }
}
