using buronet_service.Models.DTOs.User; // DTOs
using System; // For Guid
using System.Collections.Generic;
using System.Threading.Tasks;

namespace buronet_service.Services // Ensure this namespace is correct
{
    public interface IPostService
    {
        // Post CRUD
        Task<IEnumerable<PostDto>> GetAllPostsAsync(Guid? currentUserId);
        Task<PostDto?> GetPostByIdAsync(int postId, Guid? currentUserId);
        Task<PostDto?> CreatePostAsync(Guid userId, CreatePostDto createDto);
        Task<PostDto?> UpdatePostAsync(int postId, Guid userId, UpdatePostDto updateDto);
        Task<bool> DeletePostAsync(int postId, Guid userId);

        // Comments
        Task<CommentDto?> AddCommentAsync(int postId, Guid userId, CreateCommentDto createDto);
        Task<bool> DeleteCommentAsync(int commentId, Guid userId); // Can delete own comment

        // Likes
        Task<bool> ToggleLikeAsync(int postId, Guid userId); // Like or Unlike
        Task<bool> HasUserLikedPostAsync(int postId, Guid userId); // Helper for internal use, though DTO handles it
        Task<IEnumerable<TagWithTotalCountDto>> GetTrendingTagsAsync(int limit); // Helper for internal use, though DTO handles it
    }
}