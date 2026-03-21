using buronet_service.Data;
using buronet_service.Helpers;
using buronet_service.Models.DTOs;
using buronet_service.Models.DTOs.User;
using buronet_service.Models.User;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Security.Cryptography;
using System.Text;

namespace buronet_service.Services
{
    public class AuthService
    {
        private const string PasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$%";
        private const int MinimumPasswordLength = 8;
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly IConfiguration _configuration;

        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
        private static readonly TimeSpan EmailConfirmationTokenLifetime = TimeSpan.FromDays(7);

        public AuthService(AppDbContext context, JwtService jwt, IConfiguration configuration)
        {
            _context = context;
            _jwt = jwt;
            _configuration = configuration;
        }

        public async Task<RegisterResultDto> RegisterAsync(RegisterDto dto)
        {
            // NOTE: registration returns only access token currently
            // (can be extended later if you want auto-remember after register)
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Invalid registration details."
                };
            }

            var username = dto.Username.Trim();
            var email = dto.Email.Trim();

            if (!TryValidatePasswordStrength(dto.Password, out var passwordError))
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = passwordError
                };
            }

            var usernameExists = await _context.Users.AsNoTracking().AnyAsync(u => u.Username == username);
            var emailExists = await _context.Users.AsNoTracking().AnyAsync(u => u.Email == email);
            var pendingEmailExists = await _context.PendingUsers.AsNoTracking().AnyAsync(u => u.Email == email);

            if (usernameExists && emailExists)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "User account exists, please log in!"
                };
            }

            if (usernameExists)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Username already exists!"
                };
            }

            if (emailExists || pendingEmailExists)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Email already linked to another account!"
                };
            }

            PasswordHasher.CreateHash(dto.Password, out byte[] hash, out byte[] salt);

            // Generate confirmation token
            var confirmationTokenPlain = GenerateSecureTokenString(32);
            var confirmationTokenHash = HashToken(confirmationTokenPlain);

            var pendingUser = new PendingUser
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                ConfirmationTokenHash = confirmationTokenHash,
                CreatedAt = DateTime.UtcNow,
                TokenExpiresAt = DateTime.UtcNow.Add(EmailConfirmationTokenLifetime) // 7 days
            };

            _context.PendingUsers.Add(pendingUser);
            await _context.SaveChangesAsync();

            // Send confirmation email
            await SendConfirmationEmailAsync(email, username, confirmationTokenPlain);

            return new RegisterResultDto
            {
                Success = true,
                Message = "Registration successful. Please check your email to confirm your account."
            };
        }

        public async Task<LoginResultDto?> LoginAsync(LoginDto dto, string? ipAddress = null)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Password))
            {
                return null;
            }

            var loginIdentifier = (dto.Username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(loginIdentifier))
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username == loginIdentifier || u.Email == loginIdentifier);

            if (user == null)
            {
                // Check if user is pending (not yet confirmed)
                var pendingUser = await _context.PendingUsers.FirstOrDefaultAsync(pu =>
                    pu.Username == loginIdentifier || pu.Email == loginIdentifier);

                if (pendingUser != null)
                {
                    // User exists but hasn't confirmed email yet
                    return new LoginResultDto 
                    { 
                        Success = false,
                        Message = "Please confirm your email address before logging in. Check your inbox for the confirmation link."
                    };
                }

                return null;
            }

            if (!PasswordHasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

            // Check if email is confirmed
            if (!user.IsEmailConfirmed)
            {
                return new LoginResultDto 
                { 
                    Success = false,
                    Message = "Your email is not confirmed. Please check your inbox for the confirmation link."
                };
            }

            var accessToken = _jwt.GenerateToken(user);

            // Default session: only access token (2 days)
            if (!dto.RememberMe)
            {
                return new LoginResultDto 
                { 
                    Success = true,
                    Token = accessToken 
                };
            }

            // RememberMe: also issue refresh token
            var refreshTokenPlain = GenerateSecureTokenString(64);
            var refreshTokenHash = HashToken(refreshTokenPlain);

            var refreshEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
                CreatedByIp = ipAddress
            };

            _context.RefreshTokens.Add(refreshEntity);
            await _context.SaveChangesAsync();

            return new LoginResultDto
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshTokenPlain
            };
        }

        public async Task<LoginResultDto?> RefreshAsync(string refreshToken, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return null;
            }

            var hash = HashToken(refreshToken);

            var existing = await _context.RefreshTokens
                .AsTracking()
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

            if (existing == null || !existing.IsActive)
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId);
            if (user == null)
            {
                return null;
            }

            // Rotate refresh token (recommended)
            existing.RevokedAt = DateTime.UtcNow;
            existing.RevokedByIp = ipAddress;

            var newRefreshPlain = GenerateSecureTokenString(64);
            var newRefreshHash = HashToken(newRefreshPlain);

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime),
                CreatedByIp = ipAddress
            });

            var newAccessToken = _jwt.GenerateToken(user);

            await _context.SaveChangesAsync();

            return new LoginResultDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshPlain
            };
        }

        public async Task LogoutAsync(Guid userId, string? accessToken = null)
        {
            // Revoke all active refresh tokens for this user
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            if (activeTokens.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var t in activeTokens)
            {
                t.RevokedAt = now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Security: do not reveal if user exists
            if (user == null)
            {
                return true;
            }

            var temporaryPassword = GenerateTemporaryPassword(length: 12);

            PasswordHasher.CreateHash(temporaryPassword, out byte[] passwordHash, out byte[] passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await SendTemporaryPasswordEmailAsync(user.Email, user.Username, temporaryPassword);

            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            if (dto == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                string.IsNullOrWhiteSpace(dto.NewPassword) ||
                string.IsNullOrWhiteSpace(dto.ConfirmPassword))
            {
                return false;
            }

            if (!string.Equals(dto.NewPassword, dto.ConfirmPassword, StringComparison.Ordinal))
            {
                return false;
            }

            if (string.Equals(dto.CurrentPassword, dto.NewPassword, StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryValidatePasswordStrength(dto.NewPassword, out _))
            {
                return false;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            if (!PasswordHasher.Verify(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return false;
            }

            PasswordHasher.CreateHash(dto.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            var resetTokenRecord = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == resetDto.Token);

            if (resetTokenRecord == null || resetTokenRecord.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            if (!TryValidatePasswordStrength(resetDto.NewPassword, out _))
            {
                return false;
            }

            var user = resetTokenRecord.User;
            PasswordHasher.CreateHash(resetDto.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.UpdatedAt = DateTime.UtcNow;

            _context.PasswordResetTokens.Remove(resetTokenRecord);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ConfirmEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var tokenHash = HashToken(token);

            var pendingUser = await _context.PendingUsers
                .AsTracking()
                .FirstOrDefaultAsync(pu => pu.ConfirmationTokenHash == tokenHash);

            if (pendingUser == null)
            {
                return false;
            }

            // Check if token has expired
            if (pendingUser.TokenExpiresAt < DateTime.UtcNow)
            {
                // Delete expired pending user
                _context.PendingUsers.Remove(pendingUser);
                await _context.SaveChangesAsync();
                return false;
            }

            // Check if already confirmed
            if (pendingUser.ConfirmedAt.HasValue)
            {
                return false;
            }

            // Create actual user from pending user
            var user = new User
            {
                Id = pendingUser.Id,
                Username = pendingUser.Username,
                Email = pendingUser.Email,
                PasswordHash = pendingUser.PasswordHash,
                PasswordSalt = pendingUser.PasswordSalt,
                IsEmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Create user profile
            var userProfile = new UserProfile
            {
                Id = user.Id,
                FirstName = "",
                LastName = "",
                DateOfBirth = null,
                PhoneNumber = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserProfiles.Add(userProfile);

            // Mark pending user as confirmed and remove it
            pendingUser.ConfirmedAt = DateTime.UtcNow;
            _context.PendingUsers.Remove(pendingUser);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ResendConfirmationEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            email = email.Trim();

            // Find pending user by email
            var pendingUser = await _context.PendingUsers
                .AsTracking()
                .FirstOrDefaultAsync(pu => pu.Email == email);

            if (pendingUser == null)
            {
                // Security: don't reveal if email exists
                return true;
            }

            // Check if already confirmed
            if (pendingUser.ConfirmedAt.HasValue)
            {
                return true;
            }

            // Generate new confirmation token
            var confirmationTokenPlain = GenerateSecureTokenString(32);
            var confirmationTokenHash = HashToken(confirmationTokenPlain);

            // Update pending user with new token and extended expiry
            pendingUser.ConfirmationTokenHash = confirmationTokenHash;
            pendingUser.TokenExpiresAt = DateTime.UtcNow.Add(EmailConfirmationTokenLifetime); // 7 days from now

            await _context.SaveChangesAsync();

            // Send confirmation email with new token
            await SendConfirmationEmailAsync(email, pendingUser.Username, confirmationTokenPlain);

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

        private static bool TryValidatePasswordStrength(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Password is required.";
                return false;
            }

            if (password.Length < MinimumPasswordLength)
            {
                errorMessage = $"Password must be at least {MinimumPasswordLength} characters long.";
                return false;
            }

            if (password.Contains(' '))
            {
                errorMessage = "Password must not contain spaces.";
                return false;
            }

            var hasUpper = false;
            var hasLower = false;
            var hasDigit = false;
            var hasSpecial = false;

            foreach (var c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }

            if (!hasUpper || !hasLower || !hasDigit || !hasSpecial)
            {
                errorMessage = "Password must include at least one uppercase letter, one lowercase letter, one number, and one special character.";
                return false;
            }

            return true;
        }

        private static string GenerateTemporaryPassword(int length)
        {
            if (length < 8)
            {
                length = 8;
            }

            var result = new char[length];
            for (var i = 0; i < result.Length; i++)
            {
                var index = RandomNumberGenerator.GetInt32(0, PasswordChars.Length);
                result[i] = PasswordChars[index];
            }

            return new string(result);
        }

        private async Task SendTemporaryPasswordEmailAsync(string toEmail, string username, string temporaryPassword)
        {
            var smtp = _configuration.GetSection("Smtp");

            var host = smtp["Host"];
            var portString = smtp["Port"];
            var smtpUsername = smtp["Username"];
            var smtpPassword = smtp["Password"];
            var enableSslString = smtp["EnableSsl"];

            var from = "admin@buronet.co.in";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portString) ||
                string.IsNullOrWhiteSpace(smtpUsername) ||
                string.IsNullOrWhiteSpace(smtpPassword) ||
                string.IsNullOrWhiteSpace(enableSslString))
            {
                throw new ApplicationException("SMTP configuration is missing.");
            }

            if (!int.TryParse(portString, out var port))
            {
                throw new ApplicationException("SMTP Port is invalid.");
            }

            _ = bool.TryParse(enableSslString, out var enableSsl);

            var subject = "[Buronet] Temporary Password";
            var body =
                $"Hi {username},\n\n" +
                "A temporary password has been generated for your account.\n\n" +
                $"Temporary password: {temporaryPassword}\n\n" +
                "Please log in using this password and change it immediately.\n\n" +
                "Regards,\n" +
                "Buronet Admin";

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();

            var socketOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : (enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private async Task SendConfirmationEmailAsync(string toEmail, string username, string confirmationToken)
        {
            var smtp = _configuration.GetSection("Smtp");

            var host = smtp["Host"];
            var portString = smtp["Port"];
            var smtpUsername = smtp["Username"];
            var smtpPassword = smtp["Password"];
            var enableSslString = smtp["EnableSsl"];
            var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:3000";

            var from = "admin@buronet.co.in";

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portString) ||
                string.IsNullOrWhiteSpace(smtpUsername) ||
                string.IsNullOrWhiteSpace(smtpPassword) ||
                string.IsNullOrWhiteSpace(enableSslString))
            {
                throw new ApplicationException("SMTP configuration is missing.");
            }

            if (!int.TryParse(portString, out var port))
            {
                throw new ApplicationException("SMTP Port is invalid.");
            }

            _ = bool.TryParse(enableSslString, out var enableSsl);

            var confirmationLink = $"{frontendUrl}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";

            var subject = "[Buronet] Confirm Your Email Address";
            var body =
                $"Hi {username},\n\n" +
                "Thank you for registering with Buronet. Please confirm your email address by clicking the link below:\n\n" +
                $"{confirmationLink}\n\n" +
                "This link will expire in 7 days.\n\n" +
                "If you did not create this account, please ignore this email.\n\n" +
                "Regards,\n" +
                "Buronet Team";

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();

            var socketOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : (enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string GenerateSecureTokenString(int byteLength)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string token)
        {
            // store only hash in DB
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
