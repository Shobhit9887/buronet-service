using buronet_service.Data;
using buronet_service.Helpers;
using buronet_service.Models.User;
using buronet_service.Models.DTOs;
using buronet_service.Models.DTOs.User;
using Microsoft.EntityFrameworkCore;

namespace buronet_service.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;

        public AuthService(AppDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        public async Task<string?> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                return null;

            PasswordHasher.CreateHash(dto.Password, out byte[] hash, out byte[] salt);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _context.Users.Add(user);

            // 2. IMMEDIATELY create the associated UserProfile entity
            // Ensure UserProfile model also has a Guid Id and other default properties
            var newProfile = new UserProfile
            {
                Id = user.Id, // CRITICAL: Use the SAME ID for the shared primary key 1:1 relationship 
                FirstName = "", // Use username as initial first name or a placeholder
                LastName = "", // Default empty
                //Email = dtoemail, // Copy email to profile if applicable
                DateOfBirth = null, // Or DateTime.MinValue, depending on nullability/defaults
                PhoneNumber = "",
                // ... set other UserProfile properties with default values or empty strings/nulls
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserProfiles.Add(newProfile);
            await _context.SaveChangesAsync();
            return _jwt.GenerateToken(user);
        }

        public async Task<string?> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
                return null;

            return _jwt.GenerateToken(user);
        }

        public async Task LogoutAsync(Guid userId, string? accessToken = null)
        {
            //_logger.LogInformation("User {UserId} is attempting to log out.", userId);

            // Here you would implement server-side invalidation:
            // 1. If using Refresh Tokens: Invalidate the refresh token associated with this user/session.
            //    This is the most common use case for a backend logout endpoint.
            //    E.g., mark a refresh token in your database as invalid or delete it.
            // 2. If using short-lived Access Tokens and a Blacklist:
            //    Add the accessToken's JTI (JWT ID) to a blacklist in your cache (Redis) or database.
            //    Subsequent requests with this token would be rejected by your JWT validation middleware.
            //    (Less common for simple JWTs, as they are stateless by design and expire quickly).

            // Example: If you had a RefreshToken entity
            // var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == accessToken);
            // if (refreshToken != null) {
            //     _context.RefreshTokens.Remove(refreshToken);
            //     await _context.SaveChangesAsync();
            // }

            //_logger.LogInformation("User {UserId} logged out successfully (server-side logic completed).", userId);
            await Task.CompletedTask; // Ensure it's an async method
        }

        public async Task<UserDto> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsAdmin = user.isAdmin
            };
        }
    }
}
