// buronet_service.Models.DTOs.User/SuggestedUserDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class PopularUserDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Headline { get; set; }

        public int MutualConnections { get; set; } // The calculated number of mutual connections
    }
}
