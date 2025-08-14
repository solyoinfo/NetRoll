using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetRoll.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int CategoryTreeId { get; set; }
        public CategoryTree CategoryTree { get; set; } = default!;

    public int? ParentCategoryId { get; set; }
    public Category? Parent { get; set; }

    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    [MaxLength(100)]
    public string? Icon { get; set; }
    [MaxLength(255)]
    public string? ImageUrl { get; set; }

        public ICollection<Category> Children { get; set; } = new List<Category>();
    }
}
