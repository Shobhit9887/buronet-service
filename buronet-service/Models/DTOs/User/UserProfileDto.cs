using System;
using System.Collections.Generic;

namespace buronet_service.Models.DTOs.User
{
    public class UserProfileDto
    {
        public string Id { get; set; } // This will be the UserProfile.Id (which is also User.Id)

        // Core user details (flattened from User entity for frontend convenience. Mapped from User entity)
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Rich Profile Fields (mapped from UserProfile entity)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? StateProvince { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public string? Headline { get; set; }
        public DateTime ProfileCreatedAt { get; set; } // Renamed to avoid clash with User.CreatedAt
        public DateTime ProfileUpdatedAt { get; set; } // Renamed to avoid clash with User.UpdatedAt

        // Nested DTOs for collections (types like UserExperienceDto are in the same namespace)
        public List<UserExperienceDto> Experiences { get; set; } = new List<UserExperienceDto>();
        public List<UserSkillDto> Skills { get; set; } = new List<UserSkillDto>();
        public List<UserEducationDto> Education { get; set; } = new List<UserEducationDto>();
        public List<UserExamAttemptDto> ExamAttempts { get; set; } = new List<UserExamAttemptDto>();
        public List<UserCoachingDto> Coaching { get; set; } = new List<UserCoachingDto>();
        public List<UserPublicationDto> Publications { get; set; } = new List<UserPublicationDto>();
        public List<UserProjectDto> Projects { get; set; } = new List<UserProjectDto>();
        public List<UserCommunityGroupDto> CommunityGroups { get; set; } = new List<UserCommunityGroupDto>();
    }
}
