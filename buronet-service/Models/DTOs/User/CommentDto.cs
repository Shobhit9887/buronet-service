using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User // All DTOs share this namespace
{
    public class CommentDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty; // To display commenter's name
        public string UserEmail { get; set; } = string.Empty; // To display commenter's email
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // --- ADD THIS NESTED USER OBJECT ---
        public PostUserDto User { get; set; } = null!; // The user who liked
        // --- END ADD ---
    }

    public class CreateCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}