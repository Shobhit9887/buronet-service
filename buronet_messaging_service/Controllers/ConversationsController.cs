// buronet_messaging_service/Controllers/ConversationsController.cs
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
    [Route("api/[controller]")] // This will make it /api/conversations
    [Authorize] // All conversation operations require authentication
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<ConversationsController> _logger;

        public ConversationsController(IConversationService conversationService, ILogger<ConversationsController> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all conversations for the authenticated user.
        /// </summary>
        /// <returns>A list of ConversationDto.</returns>
        [HttpGet] // GET /api/conversations
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetUserConversations()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("GetUserConversations: User ID claim missing or invalid for authorized user.");
                return Unauthorized("User ID not found or invalid in token.");
            }

            _logger.LogInformation("Fetching conversations for user {UserId}.", userId);
            var conversations = await _conversationService.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }

        /// <summary>
        /// Gets a specific conversation by ID for the authenticated user.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation.</param>
        /// <returns>A ConversationDto if found, otherwise 404 Not Found.</returns>
        [HttpGet("{conversationId}")] // GET /api/conversations/{conversationId}
        public async Task<ActionResult<ConversationDto>> GetConversation(int conversationId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                _logger.LogWarning("GetConversation: User ID claim missing or invalid for authorized user.");
                return Unauthorized("User ID not found or invalid in token.");
            }

            _logger.LogInformation("Fetching conversation {ConversationId} for user {UserId}.", conversationId, userId);
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);

            if (conversation == null)
            {
                _logger.LogWarning("GetConversation: Conversation {ConversationId} not found or user {UserId} is not a participant.", conversationId, userId);
                return NotFound("Conversation not found or you are not a participant.");
            }
            return Ok(conversation);
        }

        /// <summary>
        /// Creates a new conversation.
        /// </summary>
        /// <param name="createDto">DTO containing participant IDs and optional title.</param>
        /// <returns>201 Created with the new ConversationDto.</returns>
        [HttpPost] // POST /api/conversations
        public async Task<ActionResult<ConversationDto>> CreateConversation([FromBody] CreateConversationDto createDto)
        {
            var creatorIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(creatorIdString) || !Guid.TryParse(creatorIdString, out Guid creatorId))
            {
                _logger.LogWarning("CreateConversation: Creator ID claim missing or invalid for authorized user.");
                return Unauthorized("Creator ID not found or invalid in token.");
            }

            // Ensure the creator is included in the participant list if not already
            if (!createDto.ParticipantUserIds.Contains(creatorId))
            {
                createDto.ParticipantUserIds.Add(creatorId);
            }

            _logger.LogInformation("User {CreatorId} creating new conversation with participants: {ParticipantIds}", creatorId, string.Join(", ", createDto.ParticipantUserIds));

            try
            {
                var newConversation = await _conversationService.CreateConversationAsync(createDto.ParticipantUserIds, createDto.Title);
                return CreatedAtAction(nameof(GetConversation), new { conversationId = newConversation.Id }, newConversation);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("CreateConversation: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateConversation: An unexpected error occurred while creating conversation.");
                return StatusCode(500, "An unexpected error occurred while creating conversation.");
            }
        }
    }
}
