using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace buronet_messaging_service.Models.Users // All user-related entities share this namespace
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; } // Creator of the post

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public Guid? Image { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPoll { get; set; } = false;
        public int? PollId { get; set; }

        [ForeignKey("PollId")]
        public Poll? Poll { get; set; }

        [Column(TypeName = "TEXT")] // Or VARCHAR(MAX) for SQL Server, TEXT for MySQL/PostgreSQL/SQLite
        public string? TagsJson { get; set; } // Internal property for DB storage

        // Non-mapped property for easier C# usage (will be handled by AutoMapper)
        [NotMapped]
        public List<string> Tags
        {
            get => TagsJson == null ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(TagsJson) ?? new List<string>();
            set => TagsJson = JsonConvert.SerializeObject(value);
        }

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!; // The user who created this post

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}