using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.User
{
    public class PendingUser
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        [Required]
        [MaxLength(512)]
        public string ConfirmationTokenHash { get; set; } = string.Empty; // Store hash, not plain token

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime TokenExpiresAt { get; set; } // Token expires after 24 hours
        public DateTime? ConfirmedAt { get; set; } // Null until confirmed
    }
}
