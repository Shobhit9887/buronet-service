using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_messaging_service.Models.Users
{
    [Table("UserExperiences")]
    public class UserExperience
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        //[Column(TypeName = "char(36)")] // Matches UserProfile.Id type
        public Guid UserProfileId { get; set; } // Foreign key to UserProfile

        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(255)]
        public string? Organization { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }

        //[ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; } = null!; // Navigation property. Type is just UserProfile
    }
}
