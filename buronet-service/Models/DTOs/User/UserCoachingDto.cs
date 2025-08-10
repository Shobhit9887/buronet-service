using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UserCoachingDto
    {
        public int Id { get; set; }
        public string CoachingInstitute { get; set; } = string.Empty;
        public string? CourseName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
