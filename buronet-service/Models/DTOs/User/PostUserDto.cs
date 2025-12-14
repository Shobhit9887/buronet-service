namespace buronet_service.Models.DTOs.User
{
    public class PostUserDto
    {
        public Guid Id { get; set; } // Matches User.Id
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid? ProfilePictureMediaId { get; set; } // From UserProfile
        public string? Headline { get; set; } // From UserProfile
        // Add any other user details you want to display with a post
    }
}
