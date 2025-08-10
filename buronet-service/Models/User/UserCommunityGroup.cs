using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User
{
    [Table("UserCommunityGroups")]
    public class UserCommunityGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        //[Column(TypeName = "char(36)")] // Matches UserProfile.Id type
        public Guid UserProfileId { get; set; } // Foreign key to UserProfile

        [Required]
        [MaxLength(255)]
        public string GroupName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        //[ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; } = null!; // Navigation property. Type is just UserProfile
    }
}
