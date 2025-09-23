using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class CreatePollDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        [Required]
        [MinLength(2, ErrorMessage = "A poll must have at least 2 options.")]
        [MaxLength(4, ErrorMessage = "A poll cannot have more than 4 options.")]
        public List<string> Options { get; set; } = new List<string>();
    }
}