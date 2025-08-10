using System;

namespace buronet_messaging_service.Models.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public ChatUserDto Sender { get; set; } = null!; // The sender of the message
        public string? ClientId { get; set; }
    }
}