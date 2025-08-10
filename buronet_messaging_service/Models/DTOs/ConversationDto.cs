using System;
using System.Collections.Generic;
using System.Linq; // For LastMessage property

namespace buronet_messaging_service.Models.DTOs
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ConversationParticipantDto> Participants { get; set; } = new List<ConversationParticipantDto>();
        public MessageDto? LastMessage { get; set; } // The last message in the conversation
        public int UnreadCount { get; set; } // Placeholder
    }
}