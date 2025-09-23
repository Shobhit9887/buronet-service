// buronet_service/Models/User/PollVote.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models.User
{
    [Table("PollVotes")]
    public class PollVote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int PollId { get; set; }

        [Required]
        public int PollOptionId { get; set; }

        [Required]
        [Column(TypeName = "char(36)")]
        public Guid UserId { get; set; }

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PollId")]
        public Poll Poll { get; set; } = null!;

        [ForeignKey("PollOptionId")]
        public PollOption PollOption { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
