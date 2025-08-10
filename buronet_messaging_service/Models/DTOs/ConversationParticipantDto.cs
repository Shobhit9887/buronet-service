using System;

namespace buronet_messaging_service.Models.DTOs
{
    public class ConversationParticipantDto
    {
        public int ConversationId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime LastReadMessageAt { get; set; }
        public ChatUserDto User { get; set; } = null!; // The participant user
    }
}