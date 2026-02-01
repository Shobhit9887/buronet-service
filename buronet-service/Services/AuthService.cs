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
        private const int MinimumPasswordLength = 8;
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, JwtService jwt, IConfiguration configuration)
        {
            _context = context;
            _jwt = jwt;
            _configuration = configuration;
        }

        public async Task<RegisterResultDto> RegisterAsync(RegisterDto dto)
        {
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

            if (emailExists)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "Email already linked to another account!"
                };
            }

            PasswordHasher.CreateHash(dto.Password, out byte[] hash, out byte[] salt);

            var user = new User
            {
                Username = username,
                Email = email,
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

            return new RegisterResultDto
            {
                Success = true,
                Token = _jwt.GenerateToken(user),
                Message = "Registration successful."
            };
        }

        public async Task<string?> LoginAsync(LoginDto dto)
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

            if (user == null || !PasswordHasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
            {
                return null;
            }

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
    }
}
