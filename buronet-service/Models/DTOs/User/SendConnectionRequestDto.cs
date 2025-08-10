using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class SendConnectionRequestDto
    {
        [Required]
        [MinLength(36)] // GUIDs are 36 chars long
        [MaxLength(36)]
        public Guid ReceiverId { get; set; }
    }
}