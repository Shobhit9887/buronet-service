using buronet_service.Data;
using buronet_service.Helpers;
using buronet_service.Models.User;
using buronet_service.Models.DTOs;
using buronet_service.Models.DTOs.User;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

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
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
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

        //public async Task<bool> ForgotPasswordAsync(string email)
        //{
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        //    if (user == null)
        //    {
        //        //_logger.LogWarning("Forgot password attempt for non-existent email: {Email}", email);
        //        return false;
        //    }

        //    // Generate a unique, secure token
        //    var tokenBytes = new byte[32];
        //    using (var rng = RandomNumberGenerator.Create())
        //    {
        //        rng.GetBytes(tokenBytes);
        //    }
        //    var resetToken = Convert.ToBase64String(tokenBytes);
        //    var tokenExpiration = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

        //    // Store the token and expiration date in the database
        //    // Note: You would need to add PasswordResetToken and TokenExpiration properties to your User model
        //    user.PasswordResetToken = resetToken;
        //    user.ResetTokenExpires = tokenExpiration;
        //    await _context.SaveChangesAsync();

        //    // *** In a real application, you would send an email here ***
        //    //_logger.LogInformation("Password reset link for {Email}: /reset-password?token={Token}", email, resetToken);
        //    return true;
        //}

        //public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto)
        //{
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == resetDto.Token);

        //    if (user == null || user.ResetTokenExpires == null || user.ResetTokenExpires < DateTime.UtcNow)
        //    {
        //        //_logger.LogWarning("Invalid or expired password reset token received.");
        //        return false;
        //    }

        //    // Hash the new password and update the user record
        //    byte[] passwordHash, passwordSalt;
        //    //AuthService.CreatePasswordHash(resetDto.NewPassword, out passwordHash, out passwordSalt);
        //    PasswordHasher.CreateHash(resetDto.NewPassword, out passwordHash, out passwordSalt);
        //    user.PasswordHash = passwordHash;
        //    user.PasswordSalt = passwordSalt;

        //    // Clear the password reset token and expiration
        //    user.PasswordResetToken = null;
        //    user.ResetTokenExpires = null;
        //    await _context.SaveChangesAsync();

        //    //_logger.LogInformation("Password successfully reset for user with token.");
        //    return true;
        //}

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // This is a security measure to prevent user enumeration
                return true;
            }

            // Generate a unique, secure token
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var resetToken = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_'); // Make URL-safe
            var tokenExpiration = DateTime.UtcNow.AddHours(1);

            // Create a new token record in the dedicated table
            var newResetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetToken,
                ExpiresAt = tokenExpiration
            };

            _context.PasswordResetTokens.Add(newResetToken);
            await _context.SaveChangesAsync();

            //_logger.LogInformation("Password reset link for {Email}: /reset-password?token={Token}", email, resetToken);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            var resetTokenRecord = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == resetDto.Token);

            if (resetTokenRecord == null || resetTokenRecord.ExpiresAt < DateTime.UtcNow)
            {
                // Token is invalid or expired
                return false;
            }

            var user = resetTokenRecord.User;
            byte[] passwordHash, passwordSalt;
            // Assuming you have an AuthService with a CreatePasswordHash method
            PasswordHasher.CreateHash(resetDto.NewPassword, out passwordHash, out passwordSalt);            
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            // Remove the used token to prevent reuse
            _context.PasswordResetTokens.Remove(resetTokenRecord);
            await _context.SaveChangesAsync();

            //_logger.LogInformation("Password successfully reset for user.");
            return true;
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
