using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UserSkillDto
    {
        public int Id { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string? Level { get; set; }
    }
}
