using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_messaging_service.Models.Users
{
    [Table("Polls")]
    public class Poll
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;
    }
}