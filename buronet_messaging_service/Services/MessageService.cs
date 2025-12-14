using AutoMapper;
using buronet_messaging_service;
using buronet_messaging_service.Data;
using buronet_messaging_service.Models;
using buronet_messaging_service.Models.DTOs;
using buronet_messaging_service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using buronet_messaging_service.Data;

namespace buronet_messaging_service.Services
{
    public class MessageService : IMessageService
    {
        private readonly MessagingDbContext _messagingContext; // Renamed for clarity
        private readonly IMapper _mapper;
        private readonly ILogger<ConversationService> _logger;

        public MessageService(MessagingDbContext messagingContext, IMapper mapper, ILogger<ConversationService> logger)
        {
            _messagingContext = messagingContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<MessageDto> AddMessageAsync(int conversationId, Guid senderId, string content, string? clientId = null)
        {
            _logger.LogInformation("Adding message to conversation {ConversationId} from sender {SenderId}.", conversationId, senderId);

            // Verify conversation and sender exist
            var conversation = await _messagingContext.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.Participants.Any(p => p.UserId == senderId));
            if (conversation == null)
            {
                _logger.LogWarning("AddMessageAsync: Conversation {ConversationId} not found or sender {SenderId} is not a participant.", conversationId, senderId);
                throw new ArgumentException("Conversation not found or sender is not a participant.");
            }

            var sender = await _messagingContext.Users.FindAsync(senderId); // Assuming Users DbSet is available via buronet_service reference
            if (sender == null)
            {
                _logger.LogWarning("AddMessageAsync: Sender User {SenderId} not found.", senderId);
                throw new ArgumentException($"Sender user with ID {senderId} not found.");
            }

            var newMessage = new Message
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow,
                ClientId = clientId
                // Sender = sender // Can set navigation property if sender is tracked
            };

            _messagingContext.Messages.Add(newMessage);

            // Update conversation's UpdatedAt to reflect new activity
            conversation.UpdatedAt = DateTime.UtcNow;
            _messagingContext.Conversations.Update(conversation);

            await _messagingContext.SaveChangesAsync();

            // Load sender and profile for DTO mapping
            await _messagingContext.Entry(newMessage)
                          .Reference(m => m.Sender)
                          .LoadAsync();
            if (newMessage.Sender != null)
            {
                await _messagingContext.Entry(newMessage.Sender)
                              .Reference(u => u.Profile)
                              .LoadAsync();
            }

            var messageDto = _mapper.Map<MessageDto>(newMessage);
            _logger.LogInformation("Message {MessageId} added to conversation {ConversationId}.", newMessage.Id, conversationId);
            return messageDto;
        }

        public async Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, Guid userId)
        {
            _logger.LogInformation("Fetching messages for conversation {ConversationId} for user {UserId}.", conversationId, userId);

            // Verify user is a participant of the conversation
            var isParticipant = await _messagingContext.ConversationParticipants.AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);
            if (!isParticipant)
            {
                _logger.LogWarning("GetConversationMessagesAsync: User {UserId} is not a participant of Conversation {ConversationId}.", userId, conversationId);
                throw new UnauthorizedAccessException("User is not a participant of this conversation.");
            }

            var messages = await _messagingContext.Messages
                                         .Where(m => m.ConversationId == conversationId)
                                         .Include(m => m.Sender)
                                             .ThenInclude(s => s.Profile) // Include sender's profile for display
                                         .OrderBy(m => m.SentAt) // Order by time sent
                                         .ToListAsync();

            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }
    }
}