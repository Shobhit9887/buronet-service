using Microsoft.AspNetCore.Mvc;
using buronet_service.Services; // IPostService
using buronet_service.Models.DTOs.User; // PostDto, CreatePostDto, etc.
using System; // For Guid
using System.Collections.Generic;
using System.Security.Claims; // For accessing user claims
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // For [Authorize] and [AllowAnonymous

namespace buronet_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Base route: api/posts
    [Authorize] // All actions in this controller require authentication by default
    public class PostsController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly MediaService _mediaService;

        public PostsController(IPostService postService, MediaService mediaService)
        {
            _postService = postService;
            _mediaService = mediaService;
        }

        private Guid? GetCurrentUserId()
        {
            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                return userIdGuid;
            }
            return null;
        }

        // POST api/posts/report-post
        // Frontend hits: /posts/report-post (under api base in this service)
        // Body:
        // {
        //   postId: number,
        //   postUrl: string,
        //   message: string,
        //   reporter?: { id: string, email: string, username: string }
        // }
        [HttpPost("report-post")]
        [AllowAnonymous]
        public async Task<IActionResult> ReportPost([FromBody] ReportPostRequestDto request)
        {
            if (request == null) return BadRequest("Request body is required.");
            if (request.PostId <= 0) return BadRequest("postId is required.");
            if (string.IsNullOrWhiteSpace(request.PostUrl)) return BadRequest("postUrl is required.");
            if (string.IsNullOrWhiteSpace(request.Message)) return BadRequest("message is required.");

            Guid? reporterId = null;
            if (request.Reporter?.Id != null && Guid.TryParse(request.Reporter.Id, out var parsedReporterId))
            {
                reporterId = parsedReporterId;
            }

            // If user is authenticated, prefer claim-based id (more trustworthy than client payload)
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && currentUserId.Value != Guid.Empty)
            {
                reporterId = currentUserId.Value;
            }

            try
            {
                var sent = await _postService.ReportPostAsync(
                    request.PostId,
                    request.PostUrl,
                    request.Message,
                    reporterId,
                    request.Reporter?.Email,
                    request.Reporter?.Username);

                if (!sent) return NotFound("Post not found.");
                return Ok(new { success = true });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while reporting the post.", details = ex.Message });
            }
        }

        // GET api/posts
        // Gets a feed of all posts. Can be viewed by anyone.
        [HttpGet]
        [AllowAnonymous] // Override [Authorize] for public viewing
        public async Task<ActionResult<IEnumerable<PostDto>>> GetAllPosts()
        {
            // Pass current user ID (if authenticated) to service to determine IsLikedByCurrentUser
            var currentUserId = GetCurrentUserId(); // Will be null if unauthenticated
            var posts = await _postService.GetAllPostsAsync(currentUserId);
            return Ok(posts);
        }

        // GET api/posts/{id}
        // Gets a single post by ID, including its comments. Can be viewed by anyone.
        [HttpGet("{id}")]
        [AllowAnonymous] // Override [Authorize] for public viewing
        public async Task<ActionResult<PostDto>> GetPostById(int id)
        {
            var currentUserId = GetCurrentUserId();
            var post = await _postService.GetPostByIdAsync(id, currentUserId);
            if (post == null) return NotFound();
            return Ok(post);
        }

        // POST api/posts
        // Creates a new post. Requires authentication.
        [HttpPost]
        public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto createDto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");
            if (!ModelState.IsValid) return BadRequest(ModelState); // Validate DTO

            try
            {
                var newPost = await _postService.CreatePostAsync(userId.Value, createDto);
                if (newPost == null) return StatusCode(500, "Failed to create post."); // Should not be null if no exception

                return CreatedAtAction(nameof(GetPostById), new { id = newPost.Id }, newPost); // 201 Created
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message }); // Handle specific application errors (e.g., user not found)
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while creating the post.", details = ex.Message });
            }
        }

        // PUT api/posts/{id}
        // Updates an existing post (only by the owner). Requires authentication.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto updateDto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updatedPost = await _postService.UpdatePostAsync(id, userId.Value, updateDto);
                if (updatedPost == null) return NotFound("Post not found or you do not own this post.");

                return Ok(updatedPost); // Return the updated post
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while updating the post.", details = ex.Message });
            }
        }

        // DELETE api/posts/{id}
        // Deletes a post (only by the owner). Requires authentication.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            try
            {
                bool deleted = await _postService.DeletePostAsync(id, userId.Value);
                if (!deleted) return NotFound("Post not found or you do not own this post.");

                return NoContent(); // 204 No Content
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while deleting the post.", details = ex.Message });
            }
        }

        // --- Post Interactions (Comments & Likes) ---

        // POST api/posts/{id}/comments
        // Adds a comment to a post. Requires authentication.
        [HttpPost("{id}/comments")]
        public async Task<ActionResult<CommentDto>> AddComment(int id, [FromBody] CreateCommentDto createDto)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var newComment = await _postService.AddCommentAsync(id, userId.Value, createDto);
                if (newComment == null) return StatusCode(500, "Failed to add comment.");
                return CreatedAtAction(nameof(GetPostById), new { id = id }, newComment); // 201 Created
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while adding the comment.", details = ex.Message });
            }
        }

        // DELETE api/posts/{postId}/comments/{commentId}
        // Deletes a comment (only by the owner). Requires authentication.
        [HttpDelete("{postId}/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int postId, int commentId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            try
            {
                bool deleted = await _postService.DeleteCommentAsync(commentId, userId.Value);
                if (!deleted) return NotFound("Comment not found or you do not own this comment.");
                return NoContent();
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while deleting the comment.", details = ex.Message });
            }
        }

        // POST api/posts/{id}/toggle-like
        // Toggles a like on a post (like if not liked, unlike if liked). Requires authentication.
        [HttpPost("{id}/toggle-like")]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty) return Unauthorized("User not authenticated.");

            try
            {
                bool isLiked = await _postService.ToggleLikeAsync(id, userId.Value);
                // Return 200 OK with a boolean indicating current like status
                return Ok(new { isLiked = isLiked });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred while toggling the like status.", details = ex.Message });
            }
        }

        [HttpGet("trending-tags")] // GET /api/posts/trending-tags
        [AllowAnonymous] // Tags can be viewed by anyone
        public async Task<ActionResult<IEnumerable<TagWithTotalCountDto>>> GetTrendingTags([FromQuery] int limit = 10) // Changed return type
        {
            //_logger.LogInformation("Fetching top {Limit} trending tags.", limit);
            var trendingTags = await _postService.GetTrendingTagsAsync(limit);
            return Ok(trendingTags);
        }

        [HttpPost("toggle-poll-vote")]
        public async Task<ActionResult<IEnumerable<TagWithTotalCountDto>>> TogglePollVoteAsync([FromBody] PollVoteDto pollVote) // Changed return type
        {
            //_logger.LogInformation("Fetching top {Limit} trending tags.", limit);
            var toggleVote = await _postService.TogglePollVoteAsync(pollVote);
            return Ok(toggleVote);
        }

        [IgnoreAntiforgeryToken]
        [HttpPost("upload_picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty)
                return Unauthorized();

            // 1️⃣ Upload to media service (same app)
            var mediaId = await _mediaService.UploadAsync(file);

            return Ok(new
            {
                profilePictureMediaId = mediaId,
                profilePictureUrl = $"/api/media/{mediaId}"
            });
        }

        public sealed class ReportPostRequestDto
        {
            public int PostId { get; set; }
            public string PostUrl { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public ReportPostReporterDto? Reporter { get; set; }
        }

        public sealed class ReportPostReporterDto
        {
            public string? Id { get; set; }
            public string? Email { get; set; }
            public string? Username { get; set; }
        }
    }
}