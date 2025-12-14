using buronet_service.Models.DTOs.User; // DTOs
using buronet_service.Models.User;
using System; // For Guid
using System.Collections.Generic;
using System.Threading.Tasks;

namespace buronet_service.Services // Ensure this namespace is correct
{
    public interface IUserService
    {
        // User Profile Get/Update Methods
        Task<UserProfileDto?> GetUserProfileDtoByIdAsync(Guid userIdGuid);
        Task<UserProfileDto?> UpdateUserProfileAsync(Guid userIdGuid, UpdateUserProfileDto updateDto);


        // Provisioning method (if still needed here, otherwise move to AuthService entirely)
        // This is a simplified fallback for scenarios where a User exists but no Profile.
        Task<UserProfileDto?> ProvisionUserAndProfileAsync(Guid userIdGuid, string email, string username);


        // User Experience Methods
        Task<UserExperienceDto?> AddUserExperienceAsync(Guid userProfileIdGuid, UpdateUserExperienceDto dto);
        Task<bool> UpdateUserExperienceAsync(int experienceId, Guid userProfileIdGuid, UpdateUserExperienceDto dto);
        Task<bool> DeleteUserExperienceAsync(int experienceId, Guid userProfileIdGuid);

        // User Skill Methods
        Task<UserSkillDto?> AddUserSkillAsync(Guid userProfileIdGuid, UpdateUserSkillDto dto);
        Task<bool> UpdateUserSkillAsync(int skillId, Guid userProfileIdGuid, UpdateUserSkillDto dto);
        Task<bool> DeleteUserSkillAsync(int skillId, Guid userProfileIdGuid);

        // User Education Methods
        Task<UserEducationDto?> AddUserEducationAsync(Guid userProfileIdGuid, UpdateUserEducationDto dto);
        Task<bool> UpdateUserEducationAsync(int educationId, Guid userProfileIdGuid, UpdateUserEducationDto dto);
        Task<bool> DeleteUserEducationAsync(int educationId, Guid userProfileIdGuid);

        // User Exam Attempt Methods
        Task<UserExamAttemptDto?> AddUserExamAttemptAsync(Guid userProfileIdGuid, UpdateUserExamAttemptDto dto);
        Task<bool> UpdateUserExamAttemptAsync(int attemptId, Guid userProfileIdGuid, UpdateUserExamAttemptDto dto);
        Task<bool> DeleteUserExamAttemptAsync(int attemptId, Guid userProfileIdGuid);

        // User Coaching Methods
        Task<UserCoachingDto?> AddUserCoachingAsync(Guid userProfileIdGuid, UpdateUserCoachingDto dto);
        Task<bool> UpdateUserCoachingAsync(int coachingId, Guid userProfileIdGuid, UpdateUserCoachingDto dto);
        Task<bool> DeleteUserCoachingAsync(int coachingId, Guid userProfileIdGuid);

        // User Publication Methods
        Task<UserPublicationDto?> AddUserPublicationAsync(Guid userProfileIdGuid, UpdateUserPublicationDto dto);
        Task<bool> UpdateUserPublicationAsync(int publicationId, Guid userProfileIdGuid, UpdateUserPublicationDto dto);
        Task<bool> DeleteUserPublicationAsync(int publicationId, Guid userProfileIdGuid);

        // User Project Methods
        Task<UserProjectDto?> AddUserProjectAsync(Guid userProfileIdGuid, UpdateUserProjectDto dto);
        Task<bool> UpdateUserProjectAsync(int projectId, Guid userProfileIdGuid, UpdateUserProjectDto dto);
        Task<bool> DeleteUserProjectAsync(int projectId, Guid userProfileIdGuid);

        // User Community Group Methods
        Task<UserCommunityGroupDto?> AddUserCommunityGroupAsync(Guid userProfileIdGuid, UpdateUserCommunityGroupDto dto);
        Task<bool> UpdateUserCommunityGroupAsync(int groupId, Guid userProfileIdGuid, UpdateUserCommunityGroupDto dto);
        Task<bool> DeleteUserCommunityGroupAsync(int groupId, Guid userProfileIdGuid);
        Task UpdateProfilePictureAsync(Guid userId, Guid mediaId);
        string MapToDo(Guid? userProfile);

    }
}