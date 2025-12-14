using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_messaging_service.Models.Users
{
    [Table("UserCoaching")]
    public class UserCoaching
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        //[Column(TypeName = "char(36)")] // Matches UserProfile.Id type
        public Guid UserProfileId { get; set; } // Foreign key to UserProfile

        [Required]
        [MaxLength(255)]
        public string CoachingInstitute { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? CourseName { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        //[ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; } = null!; // Navigation property. Type is just UserProfile
    }
}
