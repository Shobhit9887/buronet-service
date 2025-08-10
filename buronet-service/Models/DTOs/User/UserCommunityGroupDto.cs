using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UserCommunityGroupDto
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
    }
}
