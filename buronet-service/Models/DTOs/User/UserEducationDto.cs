using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UserEducationDto
    {
        public int Id { get; set; }
        public string Degree { get; set; } = string.Empty;
        public string? Major { get; set; }
        public string Institution { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
