using System;

namespace buronet_service.Models.DTOs.User
{
    public class UserExperienceDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Organization { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
