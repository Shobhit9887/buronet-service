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

namespace buronet_service.Services
{
    public class AuthService
    {
        private const string PasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@#$%";
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, JwtService jwt, IConfiguration configuration)
        {
            _context = context;
            _jwt = jwt;
            _configuration = configuration;
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

            var newProfile = new UserProfile
            {
                Id = user.Id,
                FirstName = "",
                LastName = "",
                DateOfBirth = null,
                PhoneNumber = "",
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
            await Task.CompletedTask;
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

            // Extra server-side validations (beyond DataAnnotations)
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

            if (dto.NewPassword.Length < 6)
            {
                return false;
            }

            if (string.Equals(dto.CurrentPassword, dto.NewPassword, StringComparison.Ordinal))
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

            var user = resetTokenRecord.User;
            PasswordHasher.CreateHash(resetDto.NewPassword, out byte[] passwordHash, out byte[] passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.UpdatedAt = DateTime.UtcNow;

            _context.PasswordResetTokens.Remove(resetTokenRecord);
            await _context.SaveChangesAsync();

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

            // Port 465 = implicit SSL/TLS.
            // Otherwise prefer StartTls if SSL is enabled.
            var socketOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : (enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

            await client.ConnectAsync(host, port, socketOptions);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
