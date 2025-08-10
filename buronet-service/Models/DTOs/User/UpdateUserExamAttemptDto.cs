using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserExamAttemptDto
    {
        [Required]
        [MaxLength(255)]
        public string ExamName { get; set; } = string.Empty;

        public int? Year { get; set; }

        [MaxLength(100)]
        public string? Result { get; set; }

        public string? Remarks { get; set; }
    }
}
