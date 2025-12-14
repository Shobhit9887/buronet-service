using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_messaging_service.Models.Users
{
    [Table("UserSkills")]
    public class UserSkill
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        //[Column(TypeName = "char(36)")] // Matches UserProfile.Id type
        public Guid UserProfileId { get; set; } // Foreign key to UserProfile

        [Required]
        [MaxLength(100)]
        public string SkillName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Level { get; set; } // e.g., "Beginner", "Intermediate", "Expert"

        //[ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; } = null!; // Navigation property. Type is just UserProfile
    }
}
