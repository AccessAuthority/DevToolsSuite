using System.ComponentModel.DataAnnotations;

namespace DevToolsSuite.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
