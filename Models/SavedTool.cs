using System.ComponentModel.DataAnnotations;

namespace DevToolsSuite.Models
{
    public class SavedTool
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ToolName { get; set; } = string.Empty;

        public string InputData { get; set; } = string.Empty;
        public string OutputData { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
        public bool IsFavorite { get; set; }

        // Foreign key
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}