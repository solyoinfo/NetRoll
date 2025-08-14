using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetRoll.Models
{
    public class CategoryTree
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Owner user id (string from Identity)
        [Required]
        public string OwnerUserId { get; set; } = string.Empty;

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
