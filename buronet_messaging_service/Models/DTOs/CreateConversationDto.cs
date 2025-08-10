using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace buronet_messaging_service.Models.DTOs
{
    public class CreateConversationDto
    {
        [Required]
        public List<Guid> ParticipantUserIds { get; set; } = new List<Guid>();
        [MaxLength(255)]
        public string? Title { get; set; }
    }
}