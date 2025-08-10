using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User
{
    [Table("ConnectionRequests")]
    public class ConnectionRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "char(36)")]
        public Guid SenderId { get; set; }

        [Required]
        [Column(TypeName = "char(36)")]
        public Guid ReceiverId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Accepted", "Rejected", "Cancelled"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        //[ForeignKey("SenderId")]
        [InverseProperty("SentConnectionRequests")]
        public User Sender { get; set; } = null!;

        //[ForeignKey("ReceiverId")]
        [InverseProperty("ReceivedConnectionRequests")]
        public User Receiver { get; set; } = null!;
    }
}