using AutoMapper;
using buronet_service.Data; // Your DbContext
using buronet_service.Models.DTOs.User; // DTOs
using buronet_service.Models.User; // Entities
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq; // For LINQ operations
using System.Threading.Tasks;

namespace buronet_service.Services // Ensure this namespace is correct
{
    public class PostService : IPostService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public PostService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Helper to check if a user has liked a post (internal use)
        private async Task<bool> HasUserLikedPostInternalAsync(int postId, Guid userId)
        {
            return await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }

        // --- Post CRUD ---

        public async Task<IEnumerable<PostDto>> GetAllPostsAsync(Guid? currentUserId)
        
        {
            string? currentUserIdString = currentUserId?.ToString();

            //_logger.LogInformation("Fetching all posts from database.");

            var posts = await _context.Posts
                                      .Include(p => p.User) // Include the User who created the post
                                          .ThenInclude(u => u.Profile) // <--- CRUCIAL: Include User's Profile for PostUserDto
                                      .Include(p => p.Likes)
                                          .ThenInclude(l => l.User) // Include User for Likes (for LikeDto.User)
                                              .ThenInclude(u => u.Profile) // Include User's Profile for Like.User
                                      .Include(p => p.Comments)
                                          .ThenInclude(c => c.User) // Include User for Comments (for CommentDto.User)
                                              .ThenInclude(u => u.Profile) // Include User's Profile for Comment.User
                                      .OrderByDescending(p => p.CreatedAt) // Order by latest posts
                                      .ToListAsync();

            var postDtos = _mapper.Map<List<PostDto>>(posts);

            // Manually set IsLikedByCurrentUser for each post
            foreach (var dto in postDtos)
            {
                if (currentUserId.HasValue) // Use .HasValue for Guid?
                {
                    // Check if any like in the post's Likes collection belongs to the current user
                    dto.IsLikedByCurrentUser = dto.Likes.Any(l => l.UserId == currentUserId.Value.ToString());
                }
                else
                {
                    dto.IsLikedByCurrentUser = false; // Not liked if no current user
                }
            }

            return postDtos;
        }

        public async Task<PostDto?> GetPostByIdAsync(int postId, Guid? currentUserId)
        {
            string? currentUserIdString = currentUserId?.ToString();

            var post = await _context.Posts
                //.Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                //.ThenInclude(c => c.User) // Include commenter's data for comments
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return null;

            var postDto = _mapper.Map<PostDto>(post);

            if (currentUserIdString != null)
            {
                postDto.IsLikedByCurrentUser = await HasUserLikedPostInternalAsync(postDto.Id, (Guid)currentUserId);
            }

            // Map comments separately to ensure UserName/Email is populated
            // and sort them for consistent display
            postDto.Comments = post.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => _mapper.Map<CommentDto>(c))
                .ToList();

            return postDto;
        }

        public async Task<PostDto?> CreatePostAsync(Guid userIdGuid, CreatePostDto createDto)
        {
            string userIdString = userIdGuid.ToString();

            // Ensure the user exists before creating a post
            var userExists = await _context.Users.AnyAsync(u => u.Id == userIdGuid);
            if (!userExists)
            {
                throw new ApplicationException($"User with ID {userIdString} not found.");
            }

            var post = _mapper.Map<Post>(createDto);
            post.UserId = userIdGuid;
            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            if (post.Tags != null && post.Tags.Any())
            {
                foreach (var tagName in post.Tags)
                {
                    var existingTag = await _context.TagFrequencies
                                                    .FirstOrDefaultAsync(tf => tf.TagName.ToLower() == tagName.ToLower());

                    if (existingTag == null)
                    {
                        _context.TagFrequencies.Add(new TagFrequency
                        {
                            TagName = tagName,
                            Frequency = 1,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existingTag.Frequency++;
                        existingTag.LastUpdated = DateTime.UtcNow;
                        _context.TagFrequencies.Update(existingTag);
                    }
                }
                await _context.SaveChangesAsync(); // Save tag frequency updates
                //_logger.LogInformation("Updated tag frequencies for post {PostId}.", newPost.Id);
            }

            // Fetch the created post with user data to map to DTO (needed for UserName/Email)
            var createdPost = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == post.Id);
            return _mapper.Map<PostDto>(createdPost);
        }

        public async Task<PostDto?> UpdatePostAsync(int postId, Guid userIdGuid, UpdatePostDto updateDto)
        {
            string userIdString = userIdGuid.ToString();

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userIdGuid);
            if (post == null) return null; // Post not found or not owned by user

            _mapper.Map(updateDto, post);
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Fetch updated post with user data
            var updatedPost = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == post.Id);
            return _mapper.Map<PostDto>(updatedPost);
        }

        public async Task<bool> DeletePostAsync(int postId, Guid userIdGuid)
        {
            string userIdString = userIdGuid.ToString();

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userIdGuid);
            if (post == null) return false; // Post not found or not owned by user

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Comments ---

        public async Task<CommentDto?> AddCommentAsync(int postId, Guid userIdGuid, CreateCommentDto createDto)
        {
            string userIdString = userIdGuid.ToString();

            // Ensure post and user exist
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            var userExists = await _context.Users.AnyAsync(u => u.Id == userIdGuid);
            if (!postExists || !userExists)
            {
                throw new ApplicationException("Post or User not found.");
            }

            var comment = _mapper.Map<Comment>(createDto);
            comment.PostId = postId;
            comment.UserId = userIdGuid;
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Fetch created comment with user data (needed for UserName/Email)
            var createdComment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == comment.Id);
            return _mapper.Map<CommentDto>(createdComment);
        }

        public async Task<bool> DeleteCommentAsync(int commentId, Guid userIdGuid)
        {
            string userIdString = userIdGuid.ToString();

            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userIdGuid);
            if (comment == null) return false; // Comment not found or not owned by user

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Likes ---

        public async Task<bool> ToggleLikeAsync(int postId, Guid userIdGuid)
        {
            string userIdString = userIdGuid.ToString();

            var existingLike = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userIdGuid);

            if (existingLike != null)
            {
                // User already liked it, so unlike (delete the like)
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return false; // Indicates it was unliked
            }
            else
            {
                // User hasn't liked it, so like (add a new like)
                var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
                var userExists = await _context.Users.AnyAsync(u => u.Id == userIdGuid);
                if (!postExists || !userExists)
                {
                    throw new ApplicationException("Post or User not found for liking.");
                }

                var newLike = new Like
                {
                    PostId = postId,
                    UserId = userIdGuid,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow // Add UpdatedAt here as well
                };
                _context.Likes.Add(newLike);
                await _context.SaveChangesAsync();
                return true; // Indicates it was liked
            }
        }

        public async Task<bool> HasUserLikedPostAsync(int postId, Guid userIdGuid)
        {
            string userIdString = userIdGuid.ToString();
            return await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == userIdGuid);
        }

        //public async Task<IEnumerable<TagWithTotalCountDto>> GetTrendingTagsAsync(int limit)
        //{
        //    //_logger.LogInformation("Fetching top {Limit} trending tags from TagFrequencies table.", limit);

        //    var recentPostsTagsArrays = await _context.Posts
        //                                            .OrderByDescending(p => p.CreatedAt)
        //                                            .Take(100) // Consider "trending" based on recent activity
        //                                            .Select(p => p.TagsJson) // This will select List<string[]>
        //                                            .ToListAsync();


        //    // Flatten the List<string[]> into a single List<string>
        //    var allTagsFromRecentPosts = recentPostsTagsArrays
        //        .Where(tagsArray => tagsArray != null && tagsArray.Any()) // Filter out null or empty arrays
        //        .SelectMany(tagsArray => tagsArray) // Flatten the array of arrays
        //        .ToList();

        //    // Group, count, and order the tags
        //    var trendingTagsWithCounts = allTagsFromRecentPosts
        //        .GroupBy(tag => tag.ToLower()) // Group by lowercase tag for case-insensitivity
        //        .Select(group => new TagWithTotalCountDto
        //        {
        //            TagName = group.Key,
        //            TotalPosts = group.Count() // Count occurrences within the recent posts
        //        })
        //        .OrderByDescending(dto => dto.TotalPosts)
        //        .Take(limit)
        //        .ToList();

        //    //_logger.LogInformation("Found {Count} trending tags with counts.", trendingTagsWithCounts.Count);
        //    return trendingTagsWithCounts;
        //}

        public async Task<IEnumerable<TagWithTotalCountDto>> GetTrendingTagsAsync(int limit)
        {
            //_logger.LogInformation("Calculating trending tags with a limit of {Limit}.", limit);

            var aWeekAgo = DateTime.UtcNow.AddDays(-7);

            // Correctly fetch tags from recent posts.
            var recentPostsTagsArrays = await _context.Posts
                                                      .OrderByDescending(p => p.CreatedAt)
                                                      .Take(100)
                                                      .Select(p => p.TagsJson)
                                                      .ToListAsync();

            // Correctly deserialize all tags and flatten into a single list
            var allTagsFromRecentPosts = recentPostsTagsArrays
                .Where(json => !string.IsNullOrEmpty(json) && json != "[]")
                .SelectMany(json => JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>())
                .ToList();

            var topTrendingTags = allTagsFromRecentPosts
                .GroupBy(tag => tag.ToLower())
                .OrderByDescending(group => group.Count())
                .Take(limit)
                .Select(group => group.Key)
                .ToList();

            var trendingTagsWithInfo = new List<TagWithTotalCountDto>();

            foreach (var tag in topTrendingTags)
            {
                // For each trending tag, perform the additional queries
                var tagSearchTerm = $"\"" + tag.Replace("\"", "\\\"") + $"\"";
                var postsWithTag = _context.Posts
                                           .Where(p => p.TagsJson != null && p.TagsJson.ToLower().Contains(tagSearchTerm));

                // Get total count
                var totalPostsCount = await postsWithTag.CountAsync();

                // Get count for posts less than a week old
                var postsLastWeekCount = await postsWithTag
                                                 .Where(p => p.CreatedAt >= aWeekAgo)
                                                 .CountAsync();

                // Get the most recent post
                var mostRecentPost = await postsWithTag
                                             .OrderByDescending(p => p.CreatedAt)
                                             .Include(p => p.User)
                                                 .ThenInclude(u => u.Profile)
                                             .FirstOrDefaultAsync();

                trendingTagsWithInfo.Add(new TagWithTotalCountDto
                {
                    TagName = tag,
                    TotalPosts = totalPostsCount,
                    PostsLastWeek = postsLastWeekCount,
                    MostRecentPost = _mapper.Map<PostDto>(mostRecentPost) // Map to DTO
                });
            }

            //_logger.LogInformation("Found {Count} trending tags with detailed counts.", trendingTagsWithInfo.Count);
            return trendingTagsWithInfo;
        }
    }
}