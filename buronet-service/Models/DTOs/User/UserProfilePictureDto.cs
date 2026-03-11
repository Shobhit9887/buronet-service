namespace buronet_service.Models.DTOs.User
{
    public class UserProfilePictureDto
    {
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
