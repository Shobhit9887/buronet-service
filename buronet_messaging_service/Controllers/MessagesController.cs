// buronet_messaging_service/Controllers/MessagesController.cs
using buronet_messaging_service.Models.DTOs;
using buronet_messaging_service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace buronet_messaging_service.Controllers
{
    [ApiController]
    [Route("conversations/{conversationId}/[controller]")] // Nested route: /api/conversations/{conversationId}/messages
    [Authorize] // All message operations require authentication
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all messages for a specific conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <returns>A list of MessageDto.</returns>
        [HttpGet] // GET /api/conversations/{conversationId}/messages
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetConversationMessages(int conversationId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("GetConversationMessages: User ID claim missing or invalid for authorized user.");
                return Unauthorized("User ID not found or invalid in token.");
            }

            _logger.LogInformation("Fetching messages for conversation {ConversationId} by user {UserId}.", conversationId, userId);
            try
            {
                var messages = await _messageService.GetConversationMessagesAsync(conversationId, userId);
                return Ok(messages);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("GetConversationMessages: {ErrorMessage}", ex.Message);
                return Forbid(ex.Message); // 403 Forbidden if not a participant
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetConversationMessages: An unexpected error occurred for conversation {ConversationId}.", conversationId);
                return StatusCode(500, "An unexpected error occurred while fetching messages.");
            }
        }

        /// <summary>
        /// Sends a new message to a specific conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <param name="createDto">The content of the message.</param>
        /// <returns>201 Created with the new MessageDto.</returns>
        [HttpPost] // POST /api/conversations/{conversationId}/messages
        public async Task<ActionResult<MessageDto>> SendMessage(int conversationId, [FromBody] CreateMessageDto createDto)
        {
            var senderIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdString) || !Guid.TryParse(senderIdString, out Guid senderId))
            {
                _logger.LogWarning("SendMessage: Sender ID claim missing or invalid for authorized user.");
                return Unauthorized("Sender ID not found or invalid in token.");
            }

            if (string.IsNullOrWhiteSpace(createDto.Content))
            {
                return BadRequest("Message content cannot be empty.");
            }

            _logger.LogInformation("User {SenderId} sending message to conversation {ConversationId}.", senderId, conversationId);

            try
            {
                var newMessage = await _messageService.AddMessageAsync(conversationId, senderId, createDto.Content);
                return CreatedAtAction(nameof(GetConversationMessages), new { conversationId = conversationId }, newMessage);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("SendMessage: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("SendMessage: {ErrorMessage}", ex.Message);
                return Forbid(ex.Message); // 403 Forbidden if not a participant
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessage: An unexpected error occurred while sending message.");
                return StatusCode(500, "An unexpected error occurred while sending message.");
            }
        }

        // You can add other message-related endpoints here, e.g., DeleteMessage, MarkMessageAsRead, etc.
    }
}
