using System;

namespace buronet_service.Models.DTOs.User
{
    // DTO for displaying another user's info in a network card (e.g., "People You May Know")
    public class UserCardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Headline { get; set; } // User's designation
        public string? ProfilePictureUrl { get; set; }
        public string? CurrentOrganization { get; set; } // Not directly in schema, placeholder or custom logic
        public bool IsConnected { get; set; } // Is the current authenticated user connected to this user?
        public bool HasPendingRequestFromCurrentUser { get; set; } // Current user sent a request
        public bool HasPendingRequestToCurrentUser { get; set; } // Current user received a request
        public int MutualConnectionsCount { get; set; } // Placeholder for complex calculation
    }
}