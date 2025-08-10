using System;

namespace buronet_messaging_service.Models.DTOs
{
    // Represents a user in the context of chat (sender, participant)
    // Maps from buronet_service.Models.User.User and UserProfile
    public class ChatUserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; } // Mapped from UserProfile.ProfilePictureUrl
        // Add other relevant user fields if needed (e.g., FirstName, LastName)
    }
}