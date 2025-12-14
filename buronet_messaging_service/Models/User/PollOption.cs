// buronet_messaging_service/Models/User/PollOption.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_messaging_service.Models.Users
{
    [Table("PollOptions")]
    public class PollOption
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int PollId { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        // --- NEW: Remove Votes column. Add navigation property for PollVotes. ---
        // public int Votes { get; set; } = 0; // Remove this column
        public ICollection<PollVote> PollVotes { get; set; } = new List<PollVote>();
        // --- END NEW ---

        // Navigation properties
        [ForeignKey("PollId")]
        public Poll Poll { get; set; } = null!;
    }
}
