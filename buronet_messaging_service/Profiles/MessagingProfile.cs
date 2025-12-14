// buronet_messaging_service/Profiles/MessagingProfile.cs
using AutoMapper;
using buronet_messaging_service.Models;
using buronet_messaging_service.Models.DTOs;
using buronet_messaging_service.Models.Users; // To map from buronet_service.Models.User.User and UserProfile
using System.Linq;

namespace buronet_messaging_service.Profiles
{
    public class MessagingProfile : Profile
    {
        public MessagingProfile()
        {
            // Map User entity (from buronet_service) to ChatUserDto (for messaging context)
            CreateMap<User, ChatUserDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                // Map avatar from UserProfile.ProfilePictureUrl
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.Profile != null ? src.Profile.ProfilePictureMediaId : null));

            // Map Message entity to MessageDto
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.Sender, opt => opt.MapFrom(src => src.Sender)) // Map Sender entity to ChatUserDto
                .ForMember(dest => dest.ClientId, opt => opt.MapFrom(src => src.ClientId));

            // Map ConversationParticipant entity to ConversationParticipantDto
            CreateMap<ConversationParticipant, ConversationParticipantDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User)); // Map User entity to ChatUserDto

            // Map Conversation entity to ConversationDto
            CreateMap<Conversation, ConversationDto>()
                .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants))
                // Map LastMessage to the latest message in the conversation
                // This requires messages to be included and ordered by SentAt descending
                .ForMember(dest => dest.LastMessage, opt => opt.MapFrom(src => src.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()))
                .ForMember(dest => dest.UnreadCount, opt => opt.Ignore()); // UnreadCount is calculated client-side or in service, not directly mapped from entity
        }
    }
}
