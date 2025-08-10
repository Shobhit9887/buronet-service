using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserEducationDto
    {
        [Required]
        [MaxLength(255)]
        public string Degree { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Major { get; set; }

        [Required]
        [MaxLength(255)]
        public string Institution { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
