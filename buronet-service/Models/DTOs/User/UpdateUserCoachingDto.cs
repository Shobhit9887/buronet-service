using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserCoachingDto
    {
        [Required]
        [MaxLength(255)]
        public string CoachingInstitute { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? CourseName { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
