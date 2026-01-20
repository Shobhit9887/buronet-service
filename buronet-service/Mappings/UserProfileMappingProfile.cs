using AutoMapper;
// All entities and DTOs now share the same Models.User namespace (for entities)
// and Models.Dtos.User namespace (for DTOs)
using buronet_service.Models.User; // Import all entities from here
using buronet_service.Models.DTOs.User;

namespace buronet_service.Mappings
{
    public class UserProfileMappingProfile : Profile
    {
        public UserProfileMappingProfile()
        {
            // Core User (authentication) mapping
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Headline, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.Headline : null))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.FirstName : null))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.LastName : null))
                .ReverseMap();

            // UserProfile (rich data) mapping
            // Source is UserProfile entity, destination is UserProfileDto DTO
            CreateMap<UserProfile, UserProfileDto>()
                // Map core User properties to UserProfileDto (from the nested User object)
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                // Map renamed timestamps
                .ForMember(dest => dest.ProfileCreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.ProfileUpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ReverseMap(); // Reverse map will be for rich profile fields only

            // Mapping for updating rich UserProfile data
            CreateMap<UpdateUserProfileDto, UserProfile>()
                // Ignore Id, CreatedAt, UpdatedAt on update
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // UserExperience Mappings
            CreateMap<UserExperience, UserExperienceDto>().ReverseMap();
            CreateMap<UpdateUserExperienceDto, UserExperience>();

            // UserSkill Mappings
            CreateMap<UserSkill, UserSkillDto>().ReverseMap();
            CreateMap<UpdateUserSkillDto, UserSkill>();

            // UserEducation Mappings
            CreateMap<UserEducation, UserEducationDto>().ReverseMap();
            CreateMap<UpdateUserEducationDto, UserEducation>();

            // UserExamAttempt Mappings
            CreateMap<UserExamAttempt, UserExamAttemptDto>().ReverseMap();
            CreateMap<UpdateUserExamAttemptDto, UserExamAttempt>();

            // UserCoaching Mappings
            CreateMap<UserCoaching, UserCoachingDto>().ReverseMap();
            CreateMap<UpdateUserCoachingDto, UserCoaching>();

            // UserPublication Mappings
            CreateMap<UserPublication, UserPublicationDto>().ReverseMap();
            CreateMap<UpdateUserPublicationDto, UserPublication>();

            // UserProject Mappings
            CreateMap<UserProject, UserProjectDto>().ReverseMap();
            CreateMap<UpdateUserProjectDto, UserProject>();

            // UserCommunityGroup Mappings
            CreateMap<UserCommunityGroup, UserCommunityGroupDto>().ReverseMap();
            CreateMap<UpdateUserCommunityGroupDto, UserCommunityGroup>();

            // --- New: Post Mappings ---
            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User)) // Map Post.User entity to PostDto.User (PostUserDto)
                .ForMember(dest => dest.LikesCount, opt => opt.MapFrom(src => src.Likes.Count))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src => src.Comments.Count))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments)) // Map Comments collection
                .ForMember(dest => dest.Likes, opt => opt.MapFrom(src => src.Likes)) // Map Likes collection
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags))
                //.ForMember(dest => dest.TagsJson, opt => opt.MapFrom(src => src.TagsJson))
                .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore()) // Set in service based on current user
                .ForMember(dest => dest.IsPoll, opt => opt.MapFrom(src => src.IsPoll))
                .ForMember(dest => dest.Poll, opt => opt.MapFrom(src => src.Poll));

            CreateMap<CreatePostDto, Post>();
            CreateMap<UpdatePostDto, Post>();

            //---New: Comment Mappings ---
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User)); // Map Comment.User entity to PostUserDto

            CreateMap<CreateCommentDto, Comment>();

            // --- New: Like Mappings ---
            CreateMap<Like, LikeDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

            // NEW MAPPING: Map User entity to PostUserDto
            CreateMap<User, PostUserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Profile.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Profile.LastName))
                // Map profilePictureUrl and headline from User.Profile
                .ForMember(dest => dest.ProfilePictureMediaId, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.ProfilePictureMediaId : null))
                .ForMember(dest => dest.Headline, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.Headline : null));

            // --- NEW: Connection Mappings ---
            CreateMap<Connection, ConnectionDto>()
                // For a connection, map the data of the *other* user
                .ForMember(dest => dest.ConnectedUserId, opt => opt.MapFrom((src, dest, _, context) =>
                    src.UserId1 == (Guid)context.Items["CurrentUserId"] ? src.UserId2 : src.UserId1))
                .ForMember(dest => dest.ConnectedUserName, opt => opt.MapFrom((src, dest, _, context) =>
                    src.UserId1 == (Guid)context.Items["CurrentUserId"] ? src.User2.Username : src.User1.Username))
                .ForMember(dest => dest.ConnectedUserHeadline, opt => opt.MapFrom((src, dest, _, context) =>
                    src.UserId1 == (Guid)context.Items["CurrentUserId"] ? src.User2.Profile!.Headline : src.User1.Profile!.Headline))
                .ForMember(dest => dest.ConnectedUserProfilePictureId, opt => opt.MapFrom((src, dest, _, context) =>
                    src.UserId1 == (Guid)context.Items["CurrentUserId"] ? src.User2.Profile!.ProfilePictureMediaId : src.User1.Profile!.ProfilePictureMediaId));


            // --- NEW: ConnectionRequest Mappings ---
            CreateMap<ConnectionRequest, ConnectionRequestDto>()
                //.ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender.Username))
                //.ForMember(dest => dest.SenderHeadline, opt => opt.MapFrom(src => src.Sender.Profile!.Headline))
                //.ForMember(dest => dest.SenderProfilePictureUrl, opt => opt.MapFrom(src => src.Sender.Profile!.ProfilePictureUrl))
                //.ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.Receiver.Username))
                //.ForMember(dest => dest.ReceiverHeadline, opt => opt.MapFrom(src => src.Receiver.Profile!.Headline))
                //.ForMember(dest => dest.ReceiverProfilePictureUrl, opt => opt.MapFrom(src => src.Receiver.Profile!.ProfilePictureUrl));
                .ForMember(dest => dest.Sender, opt => opt.MapFrom(src => src.Sender))
                .ForMember(dest => dest.Receiver, opt => opt.MapFrom(src => src.Receiver));

            // --- NEW: UserCardDto (for Discover People) ---
            CreateMap<User, UserCardDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Profile!.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Profile!.LastName))
                .ForMember(dest => dest.Headline, opt => opt.MapFrom(src => src.Profile!.Headline))
                .ForMember(dest => dest.ProfilePictureMediaId, opt => opt.MapFrom(src => src.Profile!.ProfilePictureMediaId))
                .ForMember(dest => dest.CurrentOrganization, opt => opt.Ignore()) // Custom logic in service if needed
                                                                                  // Connection status flags will be set in the service
                .ForMember(dest => dest.IsConnected, opt => opt.Ignore())
                .ForMember(dest => dest.HasPendingRequestFromCurrentUser, opt => opt.Ignore())
                .ForMember(dest => dest.HasPendingRequestToCurrentUser, opt => opt.Ignore())
                .ForMember(dest => dest.MutualConnectionsCount, opt => opt.Ignore());

            CreateMap<User, SuggestedUserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.FirstName : null))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.LastName : null))
                .ForMember(dest => dest.Headline, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.Headline : null))
                .ForMember(dest => dest.ProfilePictureMediaId, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.ProfilePictureMediaId : null))
                .ForMember(dest => dest.MutualConnections, opt => opt.Ignore());

            // Map Poll entity to PollDto
            CreateMap<Poll, PollDto>()
                .ForMember(dest => dest.TotalVotes, opt => opt.MapFrom(src => src.Options.Sum(o => o.PollVotes.Count))); // Map from PollOption.Votes

            // Map PollOption entity to PollOptionDto
            // This mapping is what gets sent to the frontend.
            CreateMap<PollOption, PollOptionDto>()
                .ForMember(dest => dest.HasVoted, opt => opt.Ignore());
        }
    }
}
