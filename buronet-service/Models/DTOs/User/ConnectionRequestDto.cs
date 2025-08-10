using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.DTOs.User
{
    // DTO for displaying a pending connection request
    public class ConnectionRequestDto
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        //public string SenderName { get; set; } = string.Empty;
        //public string SenderHeadline { get; set; } = string.Empty;
        //public string? SenderProfilePictureUrl { get; set; }
        public string ReceiverId { get; set; } = string.Empty;
        //public string ReceiverName { get; set; } = string.Empty;
        //public string ReceiverHeadline { get; set; } = string.Empty;
        //public string? ReceiverProfilePictureUrl { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public UserDto Sender { get; set; } = null!;

        //[ForeignKey("ReceiverId")]
        public UserDto Receiver { get; set; } = null!;
    }
}