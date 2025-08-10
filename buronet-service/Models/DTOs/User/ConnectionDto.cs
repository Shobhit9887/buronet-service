using System;

namespace buronet_service.Models.DTOs.User
{
    // DTO for displaying an established connection
    public class ConnectionDto
    {
        public int Id { get; set; }
        public Guid ConnectedUserId { get; set; } // The ID of the connected person (not current user)
        public string ConnectedUserName { get; set; } = string.Empty;
        public string ConnectedUserHeadline { get; set; } = string.Empty;
        public string? ConnectedUserProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserProfileDto  ConnectedUser { get; set; }
    }
}