// buronet_messaging_service/Hubs/ChatHub.cs
using buronet_messaging_service.Models; // For Message entity
using buronet_messaging_service.Models.DTOs; // For Message entity
using buronet_messaging_service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace buronet_messaging_service.Hubs
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Only authenticated users can connect to this hub
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly IConversationService _conversationService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, IConversationService conversationService, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _conversationService = conversationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !Guid.TryParse(userId, out Guid parsedUserId))
            {
                _logger.LogWarning("ChatHub: Unauthorized connection attempt or invalid UserId.");
                Context.Abort(); // Abort connection if user ID is not valid
                return;
            }

            _logger.LogInformation("ChatHub: User {UserId} connected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

            // Add user to a group based on their UserId for private messaging
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

            // Add user to groups for all conversations they are part of
            var userConversations = await _conversationService.GetUserConversationIdsAsync(parsedUserId);
            foreach (var convId in userConversations)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation-{convId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("ChatHub: User {UserId} disconnected. ConnectionId: {ConnectionId}. Exception: {ExceptionMessage}", userId, Context.ConnectionId, exception?.Message);

            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // --- Hub Methods for Clients to Call ---

        /// <summary>
        /// Sends a message to a specific conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="content">The message content.</param>
        public async Task SendMessageToConversation(int conversationId, string content, string? clientId = null)
        {
            var senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (senderId == null || !Guid.TryParse(senderId, out Guid parsedSenderId))
            {
                _logger.LogWarning("SendMessageToConversation: Unauthorized sender or invalid SenderId.");
                return;
            }

            _logger.LogInformation("User {SenderId} sending message to Conversation {ConversationId}.", senderId, conversationId);

            try
            {
                // Add message to database via service
                var createdMessage = await _messageService.AddMessageAsync(conversationId, parsedSenderId, content, clientId);

                // Send message to all participants in the conversation group
                // Clients will listen for "ReceiveMessage"
                await Clients.Group($"Conversation-{conversationId}").SendAsync("ReceiveMessage", createdMessage);
                _logger.LogInformation("Message {MessageId} sent to group Conversation-{ConversationId}.", createdMessage.Id, conversationId);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("SendMessageToConversation: {ErrorMessage}", ex.Message);
                // Optionally send error back to caller
                await Clients.Caller.SendAsync("ReceiveMessageError", "Failed to send message: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessageToConversation: An unexpected error occurred.");
                await Clients.Caller.SendAsync("ReceiveMessageError", "An unexpected error occurred while sending message.");
            }
        }

        /// <summary>
        /// Creates a new conversation.
        /// </summary>
        /// <param name="createDto">DTO containing participant IDs and optional title.</param>
        public async Task CreateNewConversation(CreateConversationDto createDto)
        {
            var creatorId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (creatorId == null || !Guid.TryParse(creatorId, out Guid parsedCreatorId))
            {
                _logger.LogWarning("CreateNewConversation: Unauthorized creator or invalid CreatorId.");
                return;
            }

            _logger.LogInformation("User {CreatorId} attempting to create new conversation.", creatorId);

            try
            {
                // Add creator's ID to participants if not already there
                var allParticipantIds = new List<Guid>(createDto.ParticipantUserIds.Select(idString => idString));
                if (!allParticipantIds.Contains(parsedCreatorId))
                {
                    allParticipantIds.Add(parsedCreatorId);
                }

                var newConversation = await _conversationService.CreateConversationAsync(allParticipantIds, createDto.Title);

                // Notify all participants about the new conversation
                foreach (var participantId in allParticipantIds)
                {
                    // Add each participant to the new conversation's group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation-{newConversation.Id}"); // Add current connection to new group
                    // Notify each participant's client
                    await Clients.Group(participantId.ToString()).SendAsync("ConversationCreated", newConversation);
                }
                _logger.LogInformation("Conversation {ConversationId} created successfully.", newConversation.Id);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("CreateNewConversation: {ErrorMessage}", ex.Message);
                await Clients.Caller.SendAsync("ConversationError", "Failed to create conversation: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateNewConversation: An unexpected error occurred.");
                await Clients.Caller.SendAsync("ConversationError", "An unexpected error occurred while creating conversation.");
            }
        }
    }
}
