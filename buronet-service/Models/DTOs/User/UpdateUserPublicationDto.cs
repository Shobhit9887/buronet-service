using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserPublicationDto
    {
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? JournalConference { get; set; }

        public DateTime? PublicationDate { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        public string? Abstract { get; set; }
    }
}
