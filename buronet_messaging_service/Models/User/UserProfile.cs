using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace buronet_messaging_service.Models.Users
{
    [Table("UserProfiles")] // Matches your new database table name
    public class UserProfile
    {
        // This Id is both the Primary Key and the Foreign Key to User.Id
        [Key]
        //[Column(TypeName = "char(36)")] // Matches the CHAR(36) Id from User
        public Guid Id { get; set; } // This will be the UserId (GUID)

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? AddressLine1 { get; set; }

        [MaxLength(255)]
        public string? AddressLine2 { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? StateProvince { get; set; }

        [MaxLength(20)]
        public string? ZipCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        public Guid? ProfilePictureMediaId { get; set; }

        public string? Bio { get; set; }

        [MaxLength(255)]
        public string? Headline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property back to the core User entity (one-to-one relationship)
        [ForeignKey("Id")] // Id in UserProfiles is the FK to User.Id
        public User User { get; set; } = null!; // Type is just 'User' as it's in the same namespace

        // Navigation properties for related data (collections)
        // UserExperience etc. are now also in the same namespace
        public ICollection<UserExperience> Experiences { get; set; } = new List<UserExperience>();
        public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
        public ICollection<UserEducation> Education { get; set; } = new List<UserEducation>();
        public ICollection<UserExamAttempt> ExamAttempts { get; set; } = new List<UserExamAttempt>();
        public ICollection<UserCoaching> Coaching { get; set; } = new List<UserCoaching>();
        public ICollection<UserPublication> Publications { get; set; } = new List<UserPublication>();
        public ICollection<UserProject> Projects { get; set; } = new List<UserProject>();
        public ICollection<UserCommunityGroup> CommunityGroups { get; set; } = new List<UserCommunityGroup>();
    }
}
