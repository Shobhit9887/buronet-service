using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User // All user-related entities share this namespace
{
    [Table("Comments")]
    public class Comment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int PostId { get; set; } // Foreign key to Post

        [Required]
        //[Column(TypeName = "char(36)")] // Foreign key to User
        public Guid UserId { get; set; } // User who made the comment

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PostId")]
        public Post Post { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // The user who commented
    }
}