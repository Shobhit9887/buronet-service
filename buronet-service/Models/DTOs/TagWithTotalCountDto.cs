// buronet_service.Models.DTOs.User/TagWithTotalCountDto.cs
using buronet_service.Models.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class TagWithTotalCountDto
    {
        [Required]
        public string TagName { get; set; } = string.Empty;

        [Required]
        public int TotalPosts { get; set; }

        public int PostsLastWeek { get; set; }

        public PostDto? MostRecentPost { get; set; }

    }
}