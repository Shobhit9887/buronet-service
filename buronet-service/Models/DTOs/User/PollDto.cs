using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class PollDto
    {
        public int Id { get; set; }
        public List<PollOptionDto> Options { get; set; } = new List<PollOptionDto>();
        public int TotalVotes { get; set; }
    }

    public class PollVoteDto
    {
        [Required]
        public int PollId { get; set; }
        [Required]
        public int PollOptionId { get; set; }
        [Required]
        public Guid UserId { get; set; }
    }
}