using System.ComponentModel.DataAnnotations;

namespace NetRoll.Models;

public class UserSetting
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(450)]
    public string OwnerUserId { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Value { get; set; }
}
