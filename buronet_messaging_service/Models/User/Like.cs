using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_messaging_service.Models.Users // All user-related entities share this namespace
{
    [Table("Likes")]
    public class Like
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Primary key for the Like record itself

        [Required]
        public int PostId { get; set; } // Foreign key to Post

        [Required]
        //[Column(TypeName = "char(36)")] // Foreign key to User
        public Guid UserId { get; set; } // User who liked the post

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Added as per your request

        // Navigation properties
        [ForeignKey("PostId")]
        public Post Post { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // The user who liked
    }
}