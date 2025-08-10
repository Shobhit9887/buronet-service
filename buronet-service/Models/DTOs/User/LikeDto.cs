using System;

namespace buronet_service.Models.DTOs.User // All DTOs share this namespace
{
    public class LikeDto
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } // Added as per your request
                                                // --- ADD THIS NESTED USER OBJECT ---
        public PostUserDto User { get; set; } = null!; // The user who liked
        // --- END ADD ---
    }
}