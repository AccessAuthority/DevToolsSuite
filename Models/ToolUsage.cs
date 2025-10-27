using System.ComponentModel.DataAnnotations;

namespace DevToolsSuite.Models
{
    public class ToolUsage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ToolName { get; set; } = string.Empty;

        public string? UserId { get; set; } // Null for anonymous users
        public string? SessionId { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
        public int ProcessingTimeMs { get; set; }
        public string? UserAgent { get; set; }

        public virtual ApplicationUser? User { get; set; }
    }
}