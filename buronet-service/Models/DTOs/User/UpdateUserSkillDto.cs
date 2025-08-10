using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateUserSkillDto
    {
        [Required]
        [MaxLength(100)]
        public string SkillName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Level { get; set; }
    }
}
