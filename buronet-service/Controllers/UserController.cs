using Microsoft.AspNetCore.Mvc;
using buronet_service.Services; // Ensure this is correct for your UserService
using buronet_service.Models.DTOs.User; // Import all DTOs from this namespace
using System; // For Guid
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // For accessing user claims
using System.Collections.Generic; // For IEnumerable

namespace buronet_service.Controllers // Ensure this namespace is correct
{
    [ApiController]
    [Route("api/[controller]")] // Base route: api/users
    [Authorize] // All actions in this controller require authentication
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly MediaService _mediaService;

        // private readonly AuthService _authService; // You might inject AuthService here if ProvisionUserAndProfileAsync moves to it.

        public UsersController(IUserService userService, MediaService mediaService /*, AuthService authService */)
        {
            _userService = userService;
            _mediaService = mediaService;
            // _authService = authService;
        }

        // Helper to get the current user's ID (Guid) from their authentication claims
        private Guid? GetCurrentUserId()
        {
            // 1. Get the string value from the claim.
            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 2. Attempt to parse the string into a Guid.
            if (Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                return userIdGuid; // Successfully parsed
            }

            // Log if necessary
            // _logger.LogWarning("ClaimTypes.NameIdentifier not found or not a valid GUID for user '{UserIdentifier}'. Claim value: '{ClaimValue}'",
            //     User?.Identity?.Name ?? "Unknown", userIdString ?? "null");

            return null; // Claim not found or not a valid GUID
        }

        // GET api/users/profile
        // Fetches the complete user profile (which combines User and UserProfile data).
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetCurrentUserProfile()
        {
            var userId = GetCurrentUserId();
            // Check if userId is null or an empty GUID.
            // Guid.Empty is a valid Guid, but typically means "not set" or "invalid" in this context.
            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                return Unauthorized("Authentication ID not found or invalid.");
            }

            var userProfileDto = await _userService.GetUserProfileDtoByIdAsync(userId.Value);

            // This provisioning logic is less common for cookie auth (registration usually handles it).
            // It acts as a fallback if a user gets authenticated but their profile is somehow missing.
            // If ProvisionUserAndProfileAsync is in AuthService, you'd call _authService.ProvisionUserAndProfileAsync here.
            if (userProfileDto == null)
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value ?? $"{userId.Value}@example.com";
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? userId.Value.ToString();

                try
                {
                    // This call might attempt to register a new user, which requires a password.
                    // It's better if the initial profile creation is guaranteed at user registration time.
                    // The dummy password here is a simplified approach for fallback provisioning.
                    userProfileDto = await _userService.ProvisionUserAndProfileAsync(userId.Value, email, username);
                }
                catch (ApplicationException ex)
                {
                    // Log the exception details
                    return StatusCode(500, $"Failed to provision user profile: {ex.Message}");
                }

                if (userProfileDto == null)
                {
                    return StatusCode(500, "Failed to provision user and profile.");
                }
            }
            userProfileDto.ProfilePictureUrl = _userService.MapToDo(userProfileDto.ProfilePictureMediaId);

            return Ok(userProfileDto);
        }

        // PUT api/users/profile
        // Updates the rich profile details (only on UserProfile entity).
        [HttpPut("profile")]
        public async Task<ActionResult<UserProfileDto>> UpdateCurrentUserProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated or invalid ID.");

            var updatedProfile = await _userService.UpdateUserProfileAsync(userId.Value, updateDto);
            if (updatedProfile == null) return NotFound("User profile not found for update.");

            return Ok(updatedProfile);
        }

        // --- Endpoints for User Experiences ---
        [HttpGet("profile/experiences")]
        public async Task<ActionResult<IEnumerable<UserExperienceDto>>> GetUserExperiences()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.Experiences ?? new List<UserExperienceDto>());
        }

        [HttpPost("profile/experiences")]
        public async Task<ActionResult<UserExperienceDto>> AddUserExperience([FromBody] UpdateUserExperienceDto experienceDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newExperience = await _userService.AddUserExperienceAsync(userProfileId.Value, experienceDto);
            if (newExperience == null) return StatusCode(500, "Failed to add experience.");

            return CreatedAtAction(nameof(GetUserExperiences), new { id = newExperience.Id }, newExperience);
        }

        [HttpPut("profile/experiences/{id}")]
        public async Task<IActionResult> UpdateUserExperience(int id, [FromBody] UpdateUserExperienceDto experienceDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserExperienceAsync(id, userProfileId.Value, experienceDto);
            if (!updated) return NotFound("Experience not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/experiences/{id}")]
        public async Task<IActionResult> DeleteUserExperience(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserExperienceAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Experience not found or does not belong to user.");
            return NoContent();
        }

        // --- Endpoints for User Skills ---
        [HttpGet("profile/skills")]
        public async Task<ActionResult<IEnumerable<UserSkillDto>>> GetUserSkills()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.Skills ?? new List<UserSkillDto>());
        }

        [HttpPost("profile/skills")]
        public async Task<ActionResult<UserSkillDto>> AddUserSkill([FromBody] UpdateUserSkillDto skillDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newSkill = await _userService.AddUserSkillAsync(userProfileId.Value, skillDto);
            if (newSkill == null) return StatusCode(500, "Failed to add skill.");

            return CreatedAtAction(nameof(GetUserSkills), new { id = newSkill.Id }, newSkill);
        }

        [HttpPut("profile/skills/{id}")]
        public async Task<IActionResult> UpdateUserSkill(int id, [FromBody] UpdateUserSkillDto skillDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserSkillAsync(id, userProfileId.Value, skillDto);
            if (!updated) return NotFound("Skill not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/skills/{id}")]
        public async Task<IActionResult> DeleteUserSkill(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserSkillAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Skill not found or does not belong to user.");
            return NoContent();
        }

        // --- Endpoints for User Education ---
        [HttpGet("profile/education")]
        public async Task<ActionResult<IEnumerable<UserEducationDto>>> GetUserEducation()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.Education ?? new List<UserEducationDto>());
        }

        [HttpPost("profile/education")]
        public async Task<ActionResult<UserEducationDto>> AddUserEducation([FromBody] UpdateUserEducationDto educationDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newEducation = await _userService.AddUserEducationAsync(userProfileId.Value, educationDto);
            if (newEducation == null) return StatusCode(500, "Failed to add education.");

            return CreatedAtAction(nameof(GetUserEducation), new { id = newEducation.Id }, newEducation);
        }

        [HttpPut("profile/education/{id}")]
        public async Task<IActionResult> UpdateUserEducation(int id, [FromBody] UpdateUserEducationDto educationDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserEducationAsync(id, userProfileId.Value, educationDto);
            if (!updated) return NotFound("Education not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/education/{id}")]
        public async Task<IActionResult> DeleteUserEducation(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserEducationAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Education not found or does not belong to user.");
            return NoContent();
        }

        // --- Endpoints for User Exam Attempts ---
        [HttpGet("profile/examattempts")]
        public async Task<ActionResult<IEnumerable<UserExamAttemptDto>>> GetUserExamAttempts()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.ExamAttempts ?? new List<UserExamAttemptDto>());
        }

        [HttpPost("profile/examattempts")]
        public async Task<ActionResult<UserExamAttemptDto>> AddUserExamAttempt([FromBody] UpdateUserExamAttemptDto examAttemptDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newExamAttempt = await _userService.AddUserExamAttemptAsync(userProfileId.Value, examAttemptDto);
            if (newExamAttempt == null) return StatusCode(500, "Failed to add exam attempt.");

            return CreatedAtAction(nameof(GetUserExamAttempts), new { id = newExamAttempt.Id }, newExamAttempt);
        }

        [HttpPut("profile/examattempts/{id}")]
        public async Task<IActionResult> UpdateUserExamAttempt(int id, [FromBody] UpdateUserExamAttemptDto examAttemptDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserExamAttemptAsync(id, userProfileId.Value, examAttemptDto);
            if (!updated) return NotFound("Exam attempt not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/examattempts/{id}")]
        public async Task<IActionResult> DeleteUserExamAttempt(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserExamAttemptAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Exam attempt not found or does not belong to user.");
            return NoContent();
        }

        // --- Endpoints for User Coaching ---
        [HttpGet("profile/coaching")]
        public async Task<ActionResult<IEnumerable<UserCoachingDto>>> GetUserCoaching()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.Coaching ?? new List<UserCoachingDto>());
        }

        [HttpPost("profile/coaching")]
        public async Task<ActionResult<UserCoachingDto>> AddUserCoaching([FromBody] UpdateUserCoachingDto coachingDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newCoaching = await _userService.AddUserCoachingAsync(userProfileId.Value, coachingDto);
            if (newCoaching == null) return StatusCode(500, "Failed to add coaching.");

            return CreatedAtAction(nameof(GetUserCoaching), new { id = newCoaching.Id }, newCoaching);
        }

        [HttpPut("profile/coaching/{id}")]
        public async Task<IActionResult> UpdateUserCoaching(int id, [FromBody] UpdateUserCoachingDto coachingDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserCoachingAsync(id, userProfileId.Value, coachingDto);
            if (!updated) return NotFound("Coaching not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/coaching/{id}")]
        public async Task<IActionResult> DeleteUserCoaching(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserCoachingAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Coaching not found or does not belong to user.");
            return NoContent();
        }

        // --- Endpoints for User Publications ---
        [HttpGet("profile/publications")]
        public async Task<ActionResult<IEnumerable<UserPublicationDto>>> GetUserPublications()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.Publications ?? new List<UserPublicationDto>());
        }

        [HttpPost("profile/publications")]
        public async Task<ActionResult<UserPublicationDto>> AddUserPublication([FromBody] UpdateUserPublicationDto publicationDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newPublication = await _userService.AddUserPublicationAsync(userProfileId.Value, publicationDto);
            if (newPublication == null) return StatusCode(500, "Failed to add publication.");

            return CreatedAtAction(nameof(GetUserPublications), new { id = newPublication.Id }, newPublication);
        }

        [HttpPut("profile/publications/{id}")]
        public async Task<IActionResult> UpdateUserPublication(int id, [FromBody] UpdateUserPublicationDto publicationDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserPublicationAsync(id, userProfileId.Value, publicationDto);
            if (!updated) return NotFound("Publication not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/publications/{id}")]
        public async Task<IActionResult> DeleteUserPublication(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserPublicationAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Publication not found or does not belong to user.");
            return NoContent();
        }

        // --- Endpoints for User Projects ---
        [HttpGet("profile/projects")]
        public async Task<ActionResult<IEnumerable<UserProjectDto>>> GetUserProjects()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.Projects ?? new List<UserProjectDto>());
        }

        [HttpPost("profile/projects")]
        public async Task<ActionResult<UserProjectDto>> AddUserProject([FromBody] UpdateUserProjectDto projectDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newProject = await _userService.AddUserProjectAsync(userProfileId.Value, projectDto);
            if (newProject == null) return StatusCode(500, "Failed to add project.");

            return CreatedAtAction(nameof(GetUserProjects), new { id = newProject.Id }, newProject);
        }

        [HttpPut("profile/projects/{id}")]
        public async Task<IActionResult> UpdateUserProject(int id, [FromBody] UpdateUserProjectDto projectDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserProjectAsync(id, userProfileId.Value, projectDto);
            if (!updated) return NotFound("Project not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/projects/{id}")]
        public async Task<IActionResult> DeleteUserProject(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserProjectAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Project not found or does not belong to user.");
            return NoContent();
        }

        // --- Endpoints for User Community Groups ---
        [HttpGet("profile/communitygroups")]
        public async Task<ActionResult<IEnumerable<UserCommunityGroupDto>>> GetUserCommunityGroups()
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var userProfile = await _userService.GetUserProfileDtoByIdAsync(userProfileId.Value);
            return Ok(userProfile?.CommunityGroups ?? new List<UserCommunityGroupDto>());
        }

        [HttpPost("profile/communitygroups")]
        public async Task<ActionResult<UserCommunityGroupDto>> AddUserCommunityGroup([FromBody] UpdateUserCommunityGroupDto communityGroupDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            var newCommunityGroup = await _userService.AddUserCommunityGroupAsync(userProfileId.Value, communityGroupDto);
            if (newCommunityGroup == null) return StatusCode(500, "Failed to add community group.");

            return CreatedAtAction(nameof(GetUserCommunityGroups), new { id = newCommunityGroup.Id }, newCommunityGroup);
        }

        [HttpPut("profile/communitygroups/{id}")]
        public async Task<IActionResult> UpdateUserCommunityGroup(int id, [FromBody] UpdateUserCommunityGroupDto communityGroupDto)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool updated = await _userService.UpdateUserCommunityGroupAsync(id, userProfileId.Value, communityGroupDto);
            if (!updated) return NotFound("Community group not found or does not belong to user.");
            return NoContent();
        }

        [HttpDelete("profile/communitygroups/{id}")]
        public async Task<IActionResult> DeleteUserCommunityGroup(int id)
        {
            var userProfileId = GetCurrentUserId();
            if (!userProfileId.HasValue || userProfileId.Value == Guid.Empty) return Unauthorized();

            bool deleted = await _userService.DeleteUserCommunityGroupAsync(id, userProfileId.Value);
            if (!deleted) return NotFound("Community group not found or does not belong to user.");
            return NoContent();
        }

        [IgnoreAntiforgeryToken]
        [HttpPost("profile/upload_picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty)
                return Unauthorized();

            // 1️⃣ Upload to media service (same app)
            var mediaId = await _mediaService.UploadAsync(file);

            // 2️⃣ Save reference in user profile
            await _userService.UpdateProfilePictureAsync(userId.Value, mediaId);

            return Ok(new
            {
                profilePictureMediaId = mediaId,
                profilePictureUrl = $"/api/media/{mediaId}"
            });
        }
    }
}