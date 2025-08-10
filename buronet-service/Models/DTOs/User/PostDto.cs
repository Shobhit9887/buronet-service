using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User // All DTOs share this namespace
{
    public class PostDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty; // To display post creator's name
        public string UserEmail { get; set; } = string.Empty; // To display post creator's email
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        // --- ADD THIS NESTED USER OBJECT ---
        public PostUserDto User { get; set; } = null!; // The user who liked
        // --- END ADD ---
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; } // To indicate if the current user has liked it
        public int CommentsCount { get; set; }
        public List<CommentDto> Comments { get; set; } = new List<CommentDto>(); // For single post view
        public List<LikeDto> Likes { get; set; } = new List<LikeDto>();
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class CreatePostDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        public string? TagsJson{ get; set; } // Internal property for DB storage
    }

    public class UpdatePostDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }

}