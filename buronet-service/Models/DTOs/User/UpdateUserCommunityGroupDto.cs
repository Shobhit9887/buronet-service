using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserCommunityGroupDto
    {
        [Required]
        [MaxLength(255)]
        public string GroupName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }
    }
}
