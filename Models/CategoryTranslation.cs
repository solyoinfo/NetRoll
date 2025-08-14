using System.ComponentModel.DataAnnotations;

namespace NetRoll.Models
{
    public class CategoryTranslation
    {
        [Key]
        public int Id { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = default!;

        [Required, MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
