using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs
{
    public class ResendConfirmationEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
