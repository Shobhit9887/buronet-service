using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User
{
    public class User
    {
        [Key]
        //[Column(TypeName = "char(36)")] // Matches CHAR(36) in your database table for GUID
        public Guid Id { get; set; } // Assuming UUIDs are generated for IDs

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        //[Column(TypeName = "blob")]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Required]
        //[Column(TypeName = "blob")]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property to the rich UserProfile data (one-to-one relationship)
        // UserProfile is in the same namespace
        public UserProfile? Profile { get; set; } = null;

        // --- NEW: Add these ICollection navigation properties ---
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        //public ICollection<Connection> Connection { get; set; } = new List<Connection>();
        //public ICollection<ConnectionRequest> ConnectionRequest { get; set; } = new List<ConnectionRequest>();
        public ICollection<Connection> ConnectionsMade { get; set; } = new List<Connection>(); // Connections where this user is UserId1
        public ICollection<Connection> ConnectionsReceived { get; set; } = new List<Connection>(); // Connections where this user is UserId2
        public ICollection<ConnectionRequest> SentConnectionRequests { get; set; } = new List<ConnectionRequest>();
        public ICollection<ConnectionRequest> ReceivedConnectionRequests { get; set; } = new List<ConnectionRequest>();
        // --- END NEW ---
    }
}
