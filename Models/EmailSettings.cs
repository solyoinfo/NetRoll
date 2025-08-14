using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetRoll.Models;

public class EmailSettings
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessageResourceType = typeof(NetRoll.Validation), ErrorMessageResourceName = "Required")]
    [MaxLength(256)]
    [Display(Name = "Host")]
    [Column("Host")]
    public string SmtpHost { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    [MaxLength(256)]
    public string? Username { get; set; }

    [MaxLength(512)]
    public string? Password { get; set; }

    [MaxLength(256)]
    public string? FromName { get; set; }

    [MaxLength(256)]
    [EmailAddress]
    public string? FromEmail { get; set; }
}
