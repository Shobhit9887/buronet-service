using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserExperienceDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(255)]
        public string? Organization { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
