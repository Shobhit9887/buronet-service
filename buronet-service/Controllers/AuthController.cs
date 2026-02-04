using buronet_service.Models.DTOs;
using buronet_service.Models.DTOs.User;
using buronet_service.Services;
using Microsoft.AspNetCore.Authorization;
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
            var result = await _auth.RegisterAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { token = result.Token, message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _auth.LoginAsync(dto, ip);

            return result == null
                ? Unauthorized("Invalid credentials")
                : Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _auth.RefreshAsync(dto.RefreshToken, ip);

            return result == null
                ? Unauthorized(new { message = "Invalid or expired refresh token." })
                : Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                return Unauthorized("User ID not found or invalid in token.");
            }

            var accessToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            await _auth.LogoutAsync(userId, accessToken);

            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto forgotDto)
        {
            await _auth.ForgotPasswordAsync(forgotDto.Email);
            return Ok(new { message = "If an account with that email exists, an email has been sent." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetDto)
        {
            var success = await _auth.ResetPasswordAsync(resetDto);
            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired token." });
            }
            return Ok(new { message = "Password has been reset successfully." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "User ID not found or invalid in token." });
            }

            var success = await _auth.ChangePasswordAsync(userId, dto);
            if (!success)
            {
                return BadRequest(new { message = "Unable to change password. Please verify your inputs and try again." });
            }

            return Ok(new { message = "Password changed successfully." });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
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
