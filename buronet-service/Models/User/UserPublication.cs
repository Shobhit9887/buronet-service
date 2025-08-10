using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User
{
    [Table("UserPublications")]
    public class UserPublication
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        //[Column(TypeName = "char(36)")] // Matches UserProfile.Id type?
        public Guid UserProfileId { get; set; } // Foreign key to UserProfile

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? JournalConference { get; set; }

        public DateTime? PublicationDate { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        public string? Abstract { get; set; }

        //[ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; } = null!; // Navigation property. Type is just UserProfile
    }
}
