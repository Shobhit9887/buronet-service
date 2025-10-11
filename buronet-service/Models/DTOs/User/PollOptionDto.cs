using System;

namespace buronet_service.Models.DTOs.User
{
    public class PollOptionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Votes { get; set; }
        public bool HasVoted { get; set; }
    }
}