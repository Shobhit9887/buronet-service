using Microsoft.EntityFrameworkCore;
using buronet_service.Data; // Your DbContext
using buronet_service.Models.User; // Entities: User, UserProfile, etc.
using buronet_service.Models.DTOs.User; // DTOs: UserDto, UserProfileDto, etc.
using System; // For Guid and DateTime
using System.Threading.Tasks;
using AutoMapper;
using buronet_service.Data;
using buronet_service.Models.DTOs.User;
using buronet_service.Models.User;
using Microsoft.EntityFrameworkCore;

namespace buronet_service.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        // private readonly IAuthService _authService; // Optional: If provisioning relies on AuthService methods.

        public UserService(AppDbContext context, IMapper mapper /*, IAuthService authService */)
        {
            _context = context;
            _mapper = mapper;
            // _authService = authService;
        }

        // --- User Profile (Combined) Get/Update Methods ---

        // Get UserProfile DTO by User.Id (parameter changed to Guid)
        public async Task<UserProfileDto?> GetUserProfileDtoByIdAsync(Guid userIdGuid)
        {
            string userIdString = userIdGuid.ToString(); // Convert Guid to string for DB query

            // Eager load the UserProfile along with its related User (auth) data
            // and all nested collections
            var userProfile = await _context.UserProfiles
                .Include(up => up.User) // Include the core User entity
                .Include(up => up.Experiences)
                .Include(up => up.Skills)
                .Include(up => up.Education)
                .Include(up => up.ExamAttempts)
                .Include(up => up.Coaching)
                .Include(up => up.Publications)
                .Include(up => up.Projects)
                .Include(up => up.CommunityGroups)
                .FirstOrDefaultAsync(up => up.Id == userIdGuid); // UserProfile.Id is the UserId

            return _mapper.Map<UserProfileDto>(userProfile);
        }

        // Provision a new User (if not already existing) AND corresponding UserProfile
        // This is a simplified fallback for scenarios where a User exists but no Profile.
        // In a strict separation, this might be handled by AuthService if it involves creating the User entity.
        public async Task<UserProfileDto?> ProvisionUserAndProfileAsync(Guid userIdGuid, string email, string username)
        {
            string userIdString = userIdGuid.ToString(); // Convert Guid to string for DB query

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdGuid);

            if (existingUser == null)
            {
                // This scenario indicates a problem: an authenticated user ID without a corresponding User record.
                // In a production app, this might lead to an exception, or a more robust user creation flow.
                // For this example, we create a basic user to allow profile provisioning.
                existingUser = new User // Entity type is 'User'
                {
                    Id = userIdGuid,
                    Username = username,
                    Email = email,
                    PasswordHash = new byte[0], // Dummy hash/salt as auth is not managed here
                    PasswordSalt = new byte[0],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Users.Add(existingUser);
                await _context.SaveChangesAsync();
            }

            var existingProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.Id == userIdGuid);

            if (existingProfile == null)
            {
                var newProfile = new UserProfile // Entity type is 'UserProfile'
                {
                    Id = userIdGuid, // Link to the core User entity's Id
                    FirstName = username,
                    Headline = "New Member",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserProfiles.Add(newProfile);
                await _context.SaveChangesAsync();
            }

            // Finally, fetch the combined DTO for the response
            return await GetUserProfileDtoByIdAsync(userIdGuid);
        }

        // Update rich user profile fields (only on UserProfile entity)
        public async Task<UserProfileDto?> UpdateUserProfileAsync(Guid userIdGuid, UpdateUserProfileDto updateDto)
        {
            string userIdString = userIdGuid.ToString(); // Convert Guid to string for DB query

            var userProfile = await _context.UserProfiles.FindAsync(userIdGuid);
            if (userProfile == null)
            {
                return null;
            }

            _mapper.Map(updateDto, userProfile); // Map properties from DTO to UserProfile entity
            userProfile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            // After updating, re-fetch the full combined DTO to return
            return await GetUserProfileDtoByIdAsync(userIdGuid);
        }

        // --- User Experience Methods ---
        public async Task<UserExperienceDto?> AddUserExperienceAsync(Guid userProfileIdGuid, UpdateUserExperienceDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString(); // Convert Guid to string

            var experience = _mapper.Map<UserExperience>(dto);
            experience.UserProfileId = userProfileIdGuid; // Link to UserProfile
            _context.UserExperiences.Add(experience);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserExperienceDto>(experience);
        }

        public async Task<bool> UpdateUserExperienceAsync(int experienceId, Guid userProfileIdGuid, UpdateUserExperienceDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString(); // Convert Guid to string

            var experience = await _context.UserExperiences.FirstOrDefaultAsync(e => e.Id == experienceId && e.UserProfileId == userProfileIdGuid);
            if (experience == null) return false;

            _mapper.Map(dto, experience);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserExperienceAsync(int experienceId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString(); // Convert Guid to string

            var experience = await _context.UserExperiences.FirstOrDefaultAsync(e => e.Id == experienceId && e.UserProfileId == userProfileIdGuid);
            if (experience == null) return false;

            _context.UserExperiences.Remove(experience);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- User Skill Methods ---
        public async Task<UserSkillDto?> AddUserSkillAsync(Guid userProfileIdGuid, UpdateUserSkillDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var skill = _mapper.Map<UserSkill>(dto);
            skill.UserProfileId = userProfileIdGuid;
            _context.UserSkills.Add(skill);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserSkillDto>(skill);
        }

        public async Task<bool> UpdateUserSkillAsync(int skillId, Guid userProfileIdGuid, UpdateUserSkillDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var skill = await _context.UserSkills.FirstOrDefaultAsync(s => s.Id == skillId && s.UserProfileId == userProfileIdGuid);
            if (skill == null) return false;
            _mapper.Map(dto, skill);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserSkillAsync(int skillId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var skill = await _context.UserSkills.FirstOrDefaultAsync(s => s.Id == skillId && s.UserProfileId == userProfileIdGuid);
            if (skill == null) return false;
            _context.UserSkills.Remove(skill);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- User Education Methods ---
        public async Task<UserEducationDto?> AddUserEducationAsync(Guid userProfileIdGuid, UpdateUserEducationDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var education = _mapper.Map<UserEducation>(dto);
            education.UserProfileId = userProfileIdGuid;
            _context.UserEducation.Add(education);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserEducationDto>(education);
        }

        public async Task<bool> UpdateUserEducationAsync(int educationId, Guid userProfileIdGuid, UpdateUserEducationDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var education = await _context.UserEducation.FirstOrDefaultAsync(e => e.Id == educationId && e.UserProfileId == userProfileIdGuid);
            if (education == null) return false;
            _mapper.Map(dto, education);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserEducationAsync(int educationId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var education = await _context.UserEducation.FirstOrDefaultAsync(e => e.Id == educationId && e.UserProfileId == userProfileIdGuid);
            if (education == null) return false;
            _context.UserEducation.Remove(education);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- User Exam Attempt Methods ---
        public async Task<UserExamAttemptDto?> AddUserExamAttemptAsync(Guid userProfileIdGuid, UpdateUserExamAttemptDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var examAttempt = _mapper.Map<UserExamAttempt>(dto);
            examAttempt.UserProfileId = userProfileIdGuid;
            _context.UserExamAttempts.Add(examAttempt);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserExamAttemptDto>(examAttempt);
        }

        public async Task<bool> UpdateUserExamAttemptAsync(int attemptId, Guid userProfileIdGuid, UpdateUserExamAttemptDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var examAttempt = await _context.UserExamAttempts.FirstOrDefaultAsync(e => e.Id == attemptId && e.UserProfileId == userProfileIdGuid);
            if (examAttempt == null) return false;
            _mapper.Map(dto, examAttempt);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserExamAttemptAsync(int attemptId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var examAttempt = await _context.UserExamAttempts.FirstOrDefaultAsync(e => e.Id == attemptId && e.UserProfileId == userProfileIdGuid);
            if (examAttempt == null) return false;
            _context.UserExamAttempts.Remove(examAttempt);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- User Coaching Methods ---
        public async Task<UserCoachingDto?> AddUserCoachingAsync(Guid userProfileIdGuid, UpdateUserCoachingDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var coaching = _mapper.Map<UserCoaching>(dto);
            coaching.UserProfileId = userProfileIdGuid;
            _context.UserCoaching.Add(coaching);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserCoachingDto>(coaching);
        }

        public async Task<bool> UpdateUserCoachingAsync(int coachingId, Guid userProfileIdGuid, UpdateUserCoachingDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var coaching = await _context.UserCoaching.FirstOrDefaultAsync(c => c.Id == coachingId && c.UserProfileId == userProfileIdGuid);
            if (coaching == null) return false;
            _mapper.Map(dto, coaching);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserCoachingAsync(int coachingId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var coaching = await _context.UserCoaching.FirstOrDefaultAsync(c => c.Id == coachingId && c.UserProfileId == userProfileIdGuid);
            if (coaching == null) return false;
            _context.UserCoaching.Remove(coaching);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- User Publication Methods ---
        public async Task<UserPublicationDto?> AddUserPublicationAsync(Guid userProfileIdGuid, UpdateUserPublicationDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var publication = _mapper.Map<UserPublication>(dto);
            publication.UserProfileId = userProfileIdGuid;
            _context.UserPublications.Add(publication);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserPublicationDto>(publication);
        }

        public async Task<bool> UpdateUserPublicationAsync(int publicationId, Guid userProfileIdGuid, UpdateUserPublicationDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var publication = await _context.UserPublications.FirstOrDefaultAsync(p => p.Id == publicationId && p.UserProfileId == userProfileIdGuid);
            if (publication == null) return false;
            _mapper.Map(dto, publication);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserPublicationAsync(int publicationId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var publication = await _context.UserPublications.FirstOrDefaultAsync(p => p.Id == publicationId && p.UserProfileId == userProfileIdGuid);
            if (publication == null) return false;
            _context.UserPublications.Remove(publication);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- User Project Methods ---
        public async Task<UserProjectDto?> AddUserProjectAsync(Guid userProfileIdGuid, UpdateUserProjectDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var project = _mapper.Map<UserProject>(dto);
            project.UserProfileId = userProfileIdGuid;
            _context.UserProjects.Add(project);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserProjectDto>(project);
        }

        public async Task<bool> UpdateUserProjectAsync(int projectId, Guid userProfileIdGuid, UpdateUserProjectDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var project = await _context.UserProjects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserProfileId == userProfileIdGuid);
            if (project == null) return false;
            _mapper.Map(dto, project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserProjectAsync(int projectId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var project = await _context.UserProjects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserProfileId == userProfileIdGuid);
            if (project == null) return false;
            _context.UserProjects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- User Community Group Methods ---
        public async Task<UserCommunityGroupDto?> AddUserCommunityGroupAsync(Guid userProfileIdGuid, UpdateUserCommunityGroupDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var communityGroup = _mapper.Map<UserCommunityGroup>(dto);
            communityGroup.UserProfileId = userProfileIdGuid;
            _context.UserCommunityGroups.Add(communityGroup);
            await _context.SaveChangesAsync();
            return _mapper.Map<UserCommunityGroupDto>(communityGroup);
        }

        public async Task<bool> UpdateUserCommunityGroupAsync(int groupId, Guid userProfileIdGuid, UpdateUserCommunityGroupDto dto)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var communityGroup = await _context.UserCommunityGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.UserProfileId == userProfileIdGuid);
            if (communityGroup == null) return false;
            _mapper.Map(dto, communityGroup);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserCommunityGroupAsync(int groupId, Guid userProfileIdGuid)
        {
            string userProfileIdString = userProfileIdGuid.ToString();
            var communityGroup = await _context.UserCommunityGroups.FirstOrDefaultAsync(g => g.Id == groupId && g.UserProfileId == userProfileIdGuid);
            if (communityGroup == null) return false;
            _context.UserCommunityGroups.Remove(communityGroup);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateProfilePictureAsync(Guid userId, Guid mediaId)
        {
            var profile = await _context.UserProfiles.FindAsync(userId);
            if (profile == null)
                throw new ApplicationException("User profile not found");

            profile.ProfilePictureMediaId = mediaId;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }


        private const string MediaBaseUrl = "https://localhost:44349/api/media";

        public string MapToDo(Guid? profilePictureId)
        {
            var profilePicUrl = $"{MediaBaseUrl}/{profilePictureId}";
            return profilePicUrl;
        }

        public async Task<string?> GetMediaUrlAsync(Guid? mediaId)
        {
            if (!mediaId.HasValue || mediaId.Value == Guid.Empty)
            {
                return null;
            }

            var media = await _context.MediaFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == mediaId.Value);

            return media?.StoragePath;
        }

    }
}