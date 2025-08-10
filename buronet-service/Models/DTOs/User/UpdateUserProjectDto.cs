using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserProjectDto
    {
        [Required]
        [MaxLength(255)]
        public string ProjectName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }
    }
}
