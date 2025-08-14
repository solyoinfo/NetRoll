using System.ComponentModel.DataAnnotations;

namespace NetRoll.Models
{
    public class UserImageSettings
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string OwnerUserId { get; set; } = string.Empty;
        // Maximális engedett méretek a croppolt képekhez
        public int MaxWidth { get; set; } = 900;
        public int MaxHeight { get; set; } = 900;
    }
}
