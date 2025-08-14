using Microsoft.AspNetCore.Mvc;
using buronet_service.Services; // IConnectionService
using buronet_service.Models.DTOs.User; // DTOs
using System; // For Guid
using System.Collections.Generic;
using System.Security.Claims; // For accessing user claims
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // For [Authorize]

namespace buronet_service.Controllers // Ensure this namespace is correct
{
    [ApiController]
    [Route("api/[controller]")] // Base route: api/connections
    [Authorize] // All actions in this controller require authentication
    public class ConnectionsController : ControllerBase
    {
        private readonly IConnectionService _connectionService;
        private readonly IPostService _postService; // Assuming PostService might be needed for Network Growth
        private readonly IUserService _userService; // Assuming UserService might be needed for Joined Groups

        public ConnectionsController(IConnectionService connectionService, IPostService postService, IUserService userService) // Inject other services
        {
            _connectionService = connectionService;
            _postService = postService;
            _userService = userService;
        }

        // Helper to get the current user's ID (Guid) from their authentication claims
        private Guid? GetCurrentUserId()
        {
            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                return userIdGuid;
            }
            return null;
        }

        // GET api/connections/metrics
        // Returns the total connections, joined groups, pending requests, etc. for the current user.
        [HttpGet("metrics")]
        public async Task<ActionResult<NetworkMetricsDto>> GetNetworkMetrics()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            var metrics = await _connectionService.GetNetworkMetricsAsync(userId.Value);
            return Ok(metrics);
        }

        // GET api/connections/discover
        // Returns a list of users the current user might want to connect with ("People You May Know").
        [HttpGet("discover")]
        public async Task<ActionResult<IEnumerable<UserCardDto>>> DiscoverUsers()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            var discoverUsers = await _connectionService.GetDiscoverUsersAsync(userId.Value);
            return Ok(discoverUsers);
        }

        // GET api/connections
        // Returns a list of users the current user is connected with.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConnectionDto>>> GetConnections()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            var connections = await _connectionService.GetUserConnectionsAsync(userId.Value);
            return Ok(connections);
        }

        // GET api/connections/requests/pending
        // Returns connection requests sent TO the current user (requests they need to act on).
        [HttpGet("requests/pending")]
        public async Task<ActionResult<IEnumerable<ConnectionRequestDto>>> GetPendingRequests()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            var pendingRequests = await _connectionService.GetPendingConnectionRequestsAsync(userId.Value);
            return Ok(pendingRequests);
        }

        // POST api/connections/send-request
        // Sends a connection request to another user.
        [HttpPost("send-request")]
        public async Task<ActionResult<ConnectionRequestDto>> SendConnectionRequest([FromBody] Guid receiverId)
        {
            var sendDto = new SendConnectionRequestDto { ReceiverId = receiverId };
            var senderId = GetCurrentUserId();
            if (!senderId.HasValue || senderId.Value == Guid.Empty) return Unauthorized("User not authenticated.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var newRequest = await _connectionService.SendConnectionRequestAsync(senderId.Value, sendDto);
                if (newRequest == null) return StatusCode(500, "Failed to send request.");
                return CreatedAtAction(nameof(GetPendingRequests), new { requestId = newRequest.Id }, newRequest);
            } 
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while sending the request.", details = ex.Message });
            }
        }

        // POST api/connections/requests/{requestId}/accept
        // Accepts a pending connection request.
        [HttpPost("requests/{requestId}/accept")]
        public async Task<IActionResult> AcceptConnectionRequest(int requestId)
        {
            var receiverId = GetCurrentUserId();
            if (!receiverId.HasValue || receiverId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            try
            {
                var updateDto = new UpdateConnectionRequestStatusDto { Status = ConnectionRequestStatus.Accepted.ToString() };
                bool success = await _connectionService.UpdateConnectionRequestStatusAsync(receiverId.Value, requestId, updateDto);
                if (!success) return NotFound("Connection request not found or not pending.");
                return NoContent(); // 204 No Content
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while accepting the request.", details = ex.Message });
            }
        }

        // POST api/connections/requests/{requestId}/reject
        // Rejects a pending connection request.
        [HttpPost("requests/{requestId}/reject")]
        public async Task<IActionResult> RejectConnectionRequest(int requestId)
        {
            var receiverId = GetCurrentUserId();
            if (!receiverId.HasValue || receiverId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            try
            {
                var updateDto = new UpdateConnectionRequestStatusDto { Status = ConnectionRequestStatus.Rejected.ToString() };
                bool success = await _connectionService.UpdateConnectionRequestStatusAsync(receiverId.Value, requestId, updateDto);
                if (!success) return NotFound("Connection request not found or not pending.");
                return NoContent(); // 204 No Content
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while rejecting the request.", details = ex.Message });
            }
        }

        // DELETE api/connections/{connectedUserId}
        // Removes an established connection.
        [HttpDelete("{connectedUserId}")]
        public async Task<IActionResult> RemoveConnection(Guid connectedUserId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || currentUserId.Value == Guid.Empty) return Unauthorized("User not authenticated.");
            if (!Guid.TryParse(connectedUserId.ToString(), out Guid targetUserId)) return BadRequest("Invalid connected user ID format.");


            try
            {
                bool success = await _connectionService.RemoveConnectionAsync(currentUserId.Value, connectedUserId);
                if (!success) return NotFound("Connection not found.");
                return NoContent();
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while removing the connection.", details = ex.Message });
            }
        }

        [HttpGet("suggestions")] // GET /api/connections/suggestions
        public async Task<ActionResult<IEnumerable<SuggestedUserDto>>> GetSuggestedUsers([FromQuery] int limit = 10)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString) || !Guid.TryParse(currentUserIdString, out Guid currentUserId))
            {
                //_logger.LogWarning("GetSuggestedUsers: User ID claim missing or invalid for authorized user.");
                return Unauthorized("User ID not found or invalid in token.");
            }
            var suggestions = await _connectionService.GetSuggestedUsersAsync(currentUserId, limit);
            return Ok(suggestions);
        }

        [HttpGet("popular")] // GET /api/connections/suggestions
        public async Task<ActionResult<IEnumerable<SuggestedUserDto>>> GetPopularUsers([FromQuery] int limit = 10)
        {
            var currentUserIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdString) || !Guid.TryParse(currentUserIdString, out Guid currentUserId))
            {
                //_logger.LogWarning("GetSuggestedUsers: User ID claim missing or invalid for authorized user.");
                return Unauthorized("User ID not found or invalid in token.");
            }
            var suggestions = await _connectionService.GetPopularUsersAsync(currentUserId, limit);
            return Ok(suggestions);
        }
    }
}