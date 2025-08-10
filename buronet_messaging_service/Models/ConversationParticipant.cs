using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using buronet_service.Models.User; // Reference the User model from buronet_service

namespace buronet_messaging_service.Models
{
    [Table("ConversationParticipants")]
    public class ConversationParticipant
    {
        // Composite Primary Key: ConversationId + UserId
        [Key]
        [Column(Order = 1)]
        public int ConversationId { get; set; }

        [Key]
        [Column(Order = 2)]
        //[Column(TypeName = "char(36)")] // Matches User.Id type (Guid as string)
        public Guid UserId { get; set; } // Participant's User ID

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastReadMessageAt { get; set; } = DateTime.UtcNow; // To track read status

        // Navigation properties
        [ForeignKey("ConversationId")]
        public Conversation Conversation { get; set; } = null!;

        // IMPORTANT: EF Core needs to know how to resolve this User from buronet_service.
        // This usually works via convention if the reference is added, but sometimes explicit
        // configuration in DbContext.OnModelCreating is needed (see below).
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}