using System.ComponentModel.DataAnnotations;

namespace NetRoll.Models
{
    public class Language
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(10)]
        public string Code { get; set; } = string.Empty; // e.g., hu, en

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty; // e.g., Magyar, Angol

        public string OwnerUserId { get; set; } = string.Empty; // per-user languages
    }
}
