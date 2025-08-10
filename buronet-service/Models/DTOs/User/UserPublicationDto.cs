using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UserPublicationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? JournalConference { get; set; }
        public DateTime? PublicationDate { get; set; }
        public string? Url { get; set; }
        public string? Abstract { get; set; }
    }
}
