using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DevToolsSuite.Models
{
    public class PaymentOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderId { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PaymentId { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PlanId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "INR";

        [StringLength(20)]
        public string Status { get; set; } = "created";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }

    public class UserSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SubscriptionId { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PlanId { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "active";

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? CancelledAt { get; set; } // nullable for cancellations
        public bool CancelAtCycleEnd { get; set; } // new property for pending cancellations

        // Navigation property
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}