using System.ComponentModel.DataAnnotations;

namespace NetRoll.Models
{
    public enum ImageSourceType
    {
        Unknown = 0,
        Category = 1,
        Product = 2
    }

    public class ImageAsset
    {
        public int Id { get; set; }
        [Required]
        public string OwnerUserId { get; set; } = string.Empty;

        // Eredeti fájlnév és a tárolt fájlnév (sanitizált)
        [MaxLength(260)] public string OriginalFileName { get; set; } = string.Empty;
        [MaxLength(260)] public string FileName { get; set; } = string.Empty;

        [MaxLength(100)] public string ContentType { get; set; } = "image/jpeg";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(260)] public string? Title { get; set; }
    [MaxLength(500)] public string? AltText { get; set; }

        // Forráskapcsolat (opcionális)
        public ImageSourceType SourceType { get; set; } = ImageSourceType.Unknown;
        public int? SourceId { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}
