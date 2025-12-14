using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using buronet_messaging_service.Models.Users;

namespace buronet_messaging_service.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; } // Foreign key to Conversation

        [Required]
        [Column(TypeName = "char(36)")] // Matches User.Id type (Guid as string)
        public Guid SenderId { get; set; } // User who sent the message

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [MaxLength(40)] // UUID string is 36 chars, allow a bit more
        public string? ClientId { get; set; }

        // Navigation properties
        [ForeignKey("ConversationId")]
        public Conversation Conversation { get; set; } = null!;

        // IMPORTANT: Same as ConversationParticipant, EF Core needs to resolve this User.
        [ForeignKey("SenderId")]
        public User Sender { get; set; } = null!;
    }
}