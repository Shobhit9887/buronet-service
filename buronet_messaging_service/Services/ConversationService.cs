using AutoMapper;
using buronet_messaging_service.Data;
using buronet_messaging_service.Models;
using buronet_messaging_service.Models.DTOs;
using buronet_messaging_service.Services.Interfaces;
using buronet_service.Data;
using buronet_service.Models.User; // To access the User entity
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace buronet_messaging_service.Services
{
    public class ConversationService : IConversationService
    {
        private readonly MessagingDbContext _context;
        private readonly AppDbContext _appContext;
        private readonly IMapper _mapper;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(MessagingDbContext context, IMapper mapper, ILogger<ConversationService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ConversationDto> CreateConversationAsync(List<Guid> participantUserIds, string? title = null)
        {
            if (participantUserIds == null || participantUserIds.Count < 1)
            {
                throw new ArgumentException("Conversation must have at least one participant.");
            }

            _logger.LogInformation("Creating new conversation with participants: {ParticipantIds}", string.Join(", ", participantUserIds));

            // Validate participants exist in buronet_service's User table
            // Assuming AppDbContext is accessible or you have a way to verify users.
            // For now, we'll assume they exist based on the Guid.
            // In a real app, you'd query buronet_service's AppDbContext for User existence.

            var newConversation = new Conversation
            {
                Title = title ?? "New Chat",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(newConversation);
            await _context.SaveChangesAsync(); // Save to get the Conversation Id

            // Add participants
            foreach (var userId in participantUserIds)
            {
                var participant = new ConversationParticipant
                {
                    ConversationId = newConversation.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                };
                _context.ConversationParticipants.Add(participant); // <--- FIX: Add directly to DbSet
                await _context.SaveChangesAsync();
                newConversation.Participants.Add(participant); // Also add to navigation collection for in-memory consistency
            }

            await _context.SaveChangesAsync(); // Save participants

            // Load participants and their User data for the DTO
            await _context.Entry(newConversation)
                          .Collection(c => c.Participants)
                          .LoadAsync();
            foreach (var participant in newConversation.Participants)
            {
                await _context.Entry(participant)
                              .Reference(cp => cp.User)
                              .LoadAsync();
                if (participant.User != null)
                {
                    // If you need Profile data for ChatUser, load it here
                    await _context.Entry(participant.User)
                                  .Reference(u => u.Profile)
                                  .LoadAsync();
                }
            }

            var conversationDto = _mapper.Map<ConversationDto>(newConversation);
            _logger.LogInformation("Conversation {ConversationId} created successfully.", newConversation.Id);
            return conversationDto;
        }

        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(Guid userId)
        {
            _logger.LogInformation("Fetching conversations for user {UserId}.", userId);

            //var conversations = await _context.ConversationParticipants
            //                                  .Where(cp => cp.UserId == userId)
            //                                  .Select(cp => cp.Conversation)
            //                                  .Include(c => c.Participants)
            //                                      .ThenInclude(p => p.User)
            //                                          .ThenInclude(u => u.Profile) // Include user profile for avatar/name
            //                                  .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1)) // Get last message
            //                                      .ThenInclude(m => m.Sender)
            //                                          .ThenInclude(s => s.Profile)
            //                                  .OrderByDescending(c => c.UpdatedAt) // Order by last activity
            //                                  .ToListAsync();
            var conversations = await _context.Conversations
                                              .Include(c => c.Participants)
                                                  .ThenInclude(p => p.User)
                                                      .ThenInclude(u => u.Profile) // Include user profile for avatar/name
                                              .Where(c => c.Participants.Any(cp => cp.UserId == userId)) // Filter by current user's participation
                                              .OrderByDescending(c => c.UpdatedAt) // Order by last activity
                                              .ToListAsync(); // Materialize the conversations here

            // Manually load the last message for each conversation in a separate query/loop
            foreach (var conv in conversations)
            {
                conv.Messages = await _context.Messages
                                                       .Where(m => m.ConversationId == conv.Id)
                                                       .OrderByDescending(m => m.SentAt)
                                                       .Take(1)
                                                       .Include(m => m.Sender) // Include sender for the last message
                                                           .ThenInclude(s => s.Profile)
                                                       .ToListAsync(); // Load as a list to match ICollection<Message>

                // Manually load User data for participants and sender if not already loaded by Include
                // This manual loading is still necessary if the Includes don't fully load all needed nested User/Profile data
                // (e.g., if the initial Include chain is insufficient or if User is from a different DbContext)
                foreach (var participant in conv.Participants)
                {
                    if (participant.User == null)
                    {
                        participant.User = await _appContext.Users
                                                            .Include(u => u.Profile)
                                                            .FirstOrDefaultAsync(u => u.Id == participant.UserId)
                                                            ?? throw new InvalidOperationException($"User {participant.UserId} not found for participant in conversation {conv.Id}.");
                    }
                }
                if (conv.Messages.Any() && conv.Messages.First().Sender == null)
                {
                    conv.Messages.First().Sender = await _appContext.Users
                                                                    .Include(u => u.Profile)
                                                                    .FirstOrDefaultAsync(u => u.Id == conv.Messages.First().SenderId)
                                                                    ?? throw new InvalidOperationException($"Sender {conv.Messages.First().SenderId} not found for last message in conversation {conv.Id}.");
                }
            }


            var conversationDtos = _mapper.Map<IEnumerable<ConversationDto>>(conversations);

            foreach (var dto in conversationDtos)
            {
                // AutoMapper will now map LastMessage from conv.Messages.FirstOrDefault()
                // Ensure LastMessage is correctly mapped in MessagingProfile.cs
                dto.UnreadCount = 0; // Placeholder
            }

            return conversationDtos;
        }

        public async Task<IEnumerable<int>> GetUserConversationIdsAsync(Guid userId)
        {
            return await _context.ConversationParticipants
                                 .Where(cp => cp.UserId == userId)
                                 .Select(cp => cp.ConversationId)
                                 .ToListAsync();
        }

        public async Task<ConversationDto?> GetConversationByIdAsync(int conversationId, Guid userId)
        {
            _logger.LogInformation("Fetching conversation {ConversationId} for user {UserId}.", conversationId, userId);

            var conversation = await _context.Conversations
                                             .Include(c => c.Participants)
                                                 .ThenInclude(p => p.User)
                                                     .ThenInclude(u => u.Profile)
                                             .Include(c => c.Messages) // Include all messages for this view
                                                 .ThenInclude(m => m.Sender)
                                                     .ThenInclude(s => s.Profile)
                                             .FirstOrDefaultAsync(c => c.Id == conversationId && c.Participants.Any(cp => cp.UserId == userId));

            if (conversation == null) return null;

            return _mapper.Map<ConversationDto>(conversation);
        }
    }
}