using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UserProjectDto
    {
        public int Id { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Url { get; set; }
    }
}
