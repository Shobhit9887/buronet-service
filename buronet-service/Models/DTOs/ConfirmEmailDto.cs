using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs
{
    public class ConfirmEmailDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
