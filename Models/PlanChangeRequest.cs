using System.ComponentModel.DataAnnotations;

namespace NetRoll.Models
{
    public enum PlanChangeStatus { Pending=0, Approved=1, Rejected=2 }
    public class PlanChangeRequest
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? CurrentPlan { get; set; }
        [Required, MaxLength(100)]
        public string RequestedPlan { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Comment { get; set; }
        public PlanChangeStatus Status { get; set; } = PlanChangeStatus.Pending;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedUtc { get; set; }
        [MaxLength(450)]
        public string? ProcessedByUserId { get; set; }
    }
}
