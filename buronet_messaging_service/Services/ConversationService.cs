using AutoMapper;
using buronet_messaging_service.Data;
using buronet_messaging_service.Models;
using buronet_messaging_service.Models.DTOs;
using buronet_messaging_service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace buronet_messaging_service.Services
{
    public class ConversationService : IConversationService
    {
        private readonly MessagingDbContext _context;
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

            var newConversation = new Conversation
            {
                Title = title ?? "New Chat",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(newConversation);
            await _context.SaveChangesAsync();

            foreach (var userId in participantUserIds)
            {
                var participant = new ConversationParticipant
                {
                    ConversationId = newConversation.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow,
                    LastReadMessageAt = DateTime.UtcNow
                };

                _context.ConversationParticipants.Add(participant);
                await _context.SaveChangesAsync();
                newConversation.Participants.Add(participant);
            }

            await _context.SaveChangesAsync();

            await _context.Entry(newConversation)
                          .Collection(c => c.Participants)
                          .LoadAsync();

            foreach (var participant in newConversation.Participants)
            {
                await _context.Entry(participant).Reference(cp => cp.User).LoadAsync();
                if (participant.User != null)
                {
                    await _context.Entry(participant.User).Reference(u => u.Profile).LoadAsync();
                }
            }

            var conversationDto = _mapper.Map<ConversationDto>(newConversation);
            _logger.LogInformation("Conversation {ConversationId} created successfully.", newConversation.Id);
            return conversationDto;
        }

        public async Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(Guid userId)
        {
            _logger.LogInformation("Fetching conversations for user {UserId}.", userId);

            var conversations = await _context.Conversations
                                              .Include(c => c.Participants)
                                                  .ThenInclude(p => p.User)
                                                      .ThenInclude(u => u.Profile)
                                              .Where(c => c.Participants.Any(cp => cp.UserId == userId))
                                              .OrderByDescending(c => c.UpdatedAt)
                                              .ToListAsync();

            foreach (var conv in conversations)
            {
                conv.Messages = await _context.Messages
                                              .Where(m => m.ConversationId == conv.Id)
                                              .OrderByDescending(m => m.SentAt)
                                              .Take(1)
                                              .Include(m => m.Sender)
                                                  .ThenInclude(s => s.Profile)
                                              .ToListAsync();

                foreach (var participant in conv.Participants)
                {
                    if (participant.User == null)
                    {
                        participant.User = await _context.Users
                                                         .Include(u => u.Profile)
                                                         .FirstOrDefaultAsync(u => u.Id == participant.UserId)
                                                         ?? throw new InvalidOperationException($"User {participant.UserId} not found for participant in conversation {conv.Id}.");
                    }
                }

                if (conv.Messages.Any() && conv.Messages.First().Sender == null)
                {
                    var last = conv.Messages.First();
                    last.Sender = await _context.Users
                                                .Include(u => u.Profile)
                                                .FirstOrDefaultAsync(u => u.Id == last.SenderId)
                                                ?? throw new InvalidOperationException($"Sender {last.SenderId} not found for last message in conversation {conv.Id}.");
                }
            }

            var conversationDtos = _mapper.Map<List<ConversationDto>>(conversations);

            // Compute unread counts in SQL (SentAt > participant.LastReadMessageAt, excluding own messages)
            var conversationIds = conversations.Select(c => c.Id).ToList();

            var unreadCountByConversation = await _context.Messages
                                                         .Where(m => conversationIds.Contains(m.ConversationId) && m.SenderId != userId)
                                                         .Join(
                                                             _context.ConversationParticipants.Where(cp => cp.UserId == userId),
                                                             m => m.ConversationId,
                                                             cp => cp.ConversationId,
                                                             (m, cp) => new { m.ConversationId, m.SentAt, cp.LastReadMessageAt }
                                                         )
                                                         .Where(x => x.SentAt > x.LastReadMessageAt)
                                                         .GroupBy(x => x.ConversationId)
                                                         .Select(g => new { ConversationId = g.Key, Count = g.Count() })
                                                         .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

            foreach (var dto in conversationDtos)
            {
                dto.UnreadCount = unreadCountByConversation.TryGetValue(dto.Id, out var count) ? count : 0;
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
                                             .Include(c => c.Messages)
                                                 .ThenInclude(m => m.Sender)
                                                     .ThenInclude(s => s.Profile)
                                             .FirstOrDefaultAsync(c => c.Id == conversationId && c.Participants.Any(cp => cp.UserId == userId));

            if (conversation == null) return null;

            return _mapper.Map<ConversationDto>(conversation);
        }

        public async Task MarkConversationAsReadAsync(int conversationId, Guid userId)
        {
            var participant = await _context.ConversationParticipants
                                            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

            if (participant == null)
                throw new UnauthorizedAccessException("User is not a participant of this conversation.");

            // Set to "now" (simple + works without needing max message timestamp)
            participant.LastReadMessageAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}