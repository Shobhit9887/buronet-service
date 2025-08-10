using System.ComponentModel.DataAnnotations;

namespace buronet_messaging_service.Models.DTOs
{
    public class CreateMessageDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}