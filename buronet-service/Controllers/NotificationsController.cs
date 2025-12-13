using buronet_service.Models;
using buronet_service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

// Assuming you have an extension method to get UserId from JWT
// using Buronet.Common.Extensions; 

namespace buronet_service.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationsService _notificationsService;

        public NotificationsController(INotificationsService notificationsService)
        {
            _notificationsService = notificationsService;
        }

        // GET /api/notifications
        [HttpGet]
        public async Task<ActionResult<List<NotificationDto>>> GetNotifications()
        {
            // IMPORTANT: Get the authenticated user's ID
            // Replace with your actual method to extract the GUID from the JWT
            // Example placeholder:
            Guid currentUserId = GetUserIdFromClaims();

            var notifications = await _notificationsService.GetNotificationsAsync(currentUserId, 15);
            return Ok(notifications);
        }

        // --- INTERNAL ENDPOINT TO RECEIVE TRIGGERS ---
        [HttpPost("internal-create")]
        // Ensure proper internal security (e.g., API Key, not just [AllowAnonymous])
        public async Task<IActionResult> InternalCreateNotification([FromBody] InternalNotificationCreateDto dto)
        {
            if (!Enum.TryParse(dto.Type, true, out NotificationType type))
            {
                return BadRequest("Invalid notification type specified.");
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Title = dto.Title,
                Message = dto.Message,
                Type = type,
                RedirectUrl = dto.RedirectUrl,
                TargetId = dto.TargetId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationsService.CreateNotificationAsync(notification);

            return StatusCode(201, new { notification.Id });
        }

        // PUT /api/notifications/mark-read/{id}
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            Guid currentUserId = GetUserIdFromClaims();

            if(currentUserId == Guid.Parse("A0000000-0000-0000-0000-000000000001"))
            {
                return NotFound();
            }

            var success = await _notificationsService.MarkAsReadAsync(id, currentUserId);

            if (success) return NoContent(); // 204 Success
            return NotFound();
        }

        private Guid GetUserIdFromClaims()
        {
            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                return userIdGuid;
            }
            return Guid.Parse("A0000000-0000-0000-0000-000000000001");
        }
    }
}