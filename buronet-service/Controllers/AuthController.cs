using buronet_service.Models.DTOs;
using buronet_service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace buronet_service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;

        public AuthController(AuthService auth) => _auth = auth;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var token = await _auth.RegisterAsync(dto);
            return token == null ? BadRequest("User already exists") : Ok(new { token });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var token = await _auth.LoginAsync(dto);
            return token == null ? Unauthorized("Invalid credentials") : Ok(new { token });
        }

        [HttpPost("logout")]
        [Authorize] // Only authenticated users can trigger a server-side logout
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                //_logger.LogWarning("Logout endpoint hit by unidentifiable authorized user.");
                return Unauthorized("User ID not found or invalid in token.");
            }

            var accessToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _auth.LogoutAsync(userId, accessToken);

            //_logger.LogInformation("User {UserId} successfully processed logout request.", userId);
            return Ok(new { message = "Logged out successfully." });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Extract user id from JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid user ID in token");
            }

            var profile = await _auth.GetProfileAsync(userId);
            if (profile == null)
                return NotFound("User not found");

            return Ok(profile);
        }
    }
}
