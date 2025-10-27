using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DevToolsSuite.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? DisplayName { get; set; }
        public string? JobTitle { get; set; }
        public string? Company { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }

        [StringLength(20)]
        public string? SubscriptionPlan { get; set; } = "Free";

        public DateTime SubscriptionExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;

        // Payment fields
        [StringLength(50)]
        public string? RazorpayCustomerId { get; set; } // Keep for potential future Stripe integration

        // Navigation properties
        public virtual ICollection<SavedTool> SavedTools { get; set; } = new List<SavedTool>();
        public virtual ICollection<ToolUsage> ToolUsages { get; set; } = new List<ToolUsage>();
        public virtual ICollection<PaymentOrder> PaymentOrders { get; set; } = new List<PaymentOrder>();
        public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
    }
}