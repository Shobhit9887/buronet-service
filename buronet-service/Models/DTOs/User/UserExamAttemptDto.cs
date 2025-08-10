using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UserExamAttemptDto
    {
        public int Id { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public int? Year { get; set; }
        public string? Result { get; set; }
        public string? Remarks { get; set; }
    }
}
