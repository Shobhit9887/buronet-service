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
                                      .Include(p => p.Poll)
                                          .ThenInclude(poll => poll!.Options)
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
                    if (dto.IsPoll && dto.Poll != null)
                    {
                        var userVote = await _context.PollVotes
                            .AsNoTracking()
                            .FirstOrDefaultAsync(v => v.PollId == dto.Poll.Id && v.UserId == currentUserId.Value);

                        dto.Poll.TotalVotes = await _context.PollVotes
                            .Where(v => v.PollId == dto.Poll.Id)
                            .CountAsync();

                        foreach (var option in dto.Poll.Options)
                        {
                            var votes = await _context.PollVotes
                           .Where(v => v.PollId == dto.Poll.Id && v.PollOptionId == option.Id)
                           .CountAsync();

                            option.Votes = votes; // Set the total votes for each option
                        }

                       

                        if (userVote != null)
                        {
                            foreach (var option in dto.Poll.Options)
                            {
                                if (option.Id == userVote.PollOptionId)
                                {
                                    option.HasVoted = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    dto.IsLikedByCurrentUser = false; // Not liked if no current user
                    if (dto.IsPoll && dto.Poll != null)
                    {
                        foreach (var option in dto.Poll.Options)
                        {
                            option.HasVoted = false; // No vote if not logged in
                        }
                    }
                }
            }

            return postDtos;
        }

        public async Task<PostDto?> GetPostByIdAsync(int postId, Guid? currentUserId)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                    .ThenInclude(u => u.Profile)
                .Include(p => p.Likes)
                    .ThenInclude(l => l.User)
                        .ThenInclude(u => u.Profile)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                        .ThenInclude(u => u.Profile)
                // --- NEW: Include Poll and PollOptions ---
                .Include(p => p.Poll)
                    .ThenInclude(poll => poll!.Options)
                // --- END NEW ---
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return null;

            var postDto = _mapper.Map<PostDto>(post);

            if (currentUserId.HasValue)
            {
                postDto.IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId.Value);

                // --- NEW: Check if the current user has voted on the poll ---
                if (post.IsPoll && post.Poll != null && post.Poll.Options.Any())
                {
                    var userVote = await _context.PollVotes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(v => v.PollId == post.Poll.Id && v.UserId == currentUserId.Value);

                    if (userVote != null)
                    {
                        foreach (var option in postDto.Poll!.Options)
                        {
                            if (option.Id == userVote.PollOptionId)
                            {
                                option.HasVoted = true;
                            }
                        }
                    }
                }
            }
            else
            {
                postDto.IsLikedByCurrentUser = false;
            }

            return postDto;
        }

        public async Task<PostDto?> CreatePostAsync(Guid userIdGuid, CreatePostDto createDto)
        {
            var newPost = new Post
            {
                UserId = userIdGuid,
                Title = createDto.Title,
                Content = createDto.Content,
                ImageUrl = createDto.ImageUrl,
                IsPoll = createDto.IsPoll, // Set the IsPoll flag from the DTO
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TagsJson = createDto.TagsJson
            };

            _context.Posts.Add(newPost);
            await _context.SaveChangesAsync(); // Save the Post first to get its ID

            // --- NEW: If it's a poll, create the poll entity and link it ---
            if (createDto.IsPoll && createDto.Options.Any())
            {
                var newPoll = new Poll
                {
                    PostId = newPost.Id, // Link the poll back to the post
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var optionText in createDto.Options)
                {
                    // FIX: Removed 'Votes = 0' as the PollOption model no longer has a Votes property
                    newPoll.Options.Add(new PollOption { Text = optionText });
                }

                _context.Polls.Add(newPoll);
                await _context.SaveChangesAsync();

                newPost.PollId = newPoll.Id; // Link the post to the new poll
                _context.Posts.Update(newPost); // Update the post with the PollId
                await _context.SaveChangesAsync();
            }
            // --- END NEW ---

            // Load related User and Profile data for the DTO response
            // This is needed for AutoMapper to map the nested PostUserDto and PollDto
            await _context.Entry(newPost)
                          .Reference(p => p.User)
                          .LoadAsync();
            if (newPost.User != null)
            {
                await _context.Entry(newPost.User)
                              .Reference(u => u.Profile)
                              .LoadAsync();
            }
            // --- NEW: Load Poll and its Options for DTO mapping ---
            if (newPost.IsPoll && newPost.PollId.HasValue)
            {
                await _context.Entry(newPost)
                              .Reference(p => p.Poll)
                              .Query()
                              .Include(poll => poll!.Options)
                              .LoadAsync();
            }
            // --- END NEW ---

            var createdPostDto = _mapper.Map<PostDto>(newPost);

            //_logger.LogInformation("Post {PostId} created successfully by user {UserId}.", newPost.Id, userId);
            return createdPostDto;
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

        public async Task<PollOptionDto> TogglePollVoteAsync(PollVoteDto pollVote)
        {
            var existingVote = await _context.PollVotes.FirstOrDefaultAsync(l => l.PollId == pollVote.PollId && l.UserId == pollVote.UserId);

            var hasVoted = true;

            var newVote = new PollVote
            {
                PollId = pollVote.PollId,
                PollOptionId = pollVote.PollOptionId,
                UserId = pollVote.UserId,
                VotedAt = DateTime.UtcNow
            };

            if (existingVote != null)
            {
                if (existingVote.PollId == newVote.PollId && existingVote.PollOptionId == newVote.PollOptionId && existingVote.UserId == newVote.UserId)
                {
                    _context.PollVotes.Remove(existingVote);
                } else
                {
                    existingVote.PollOptionId = newVote.PollOptionId;
                    _context.PollVotes.Update(existingVote);
                    hasVoted = false; // Indicates it was updated
                }                
                await _context.SaveChangesAsync();
                //return true; // Indicates it was unliked
            }
            else
            {
                _context.PollVotes.Add(newVote);
                await _context.SaveChangesAsync();
                //return true; // Indicates it was unliked
            }

            var votes = await _context.PollVotes
                .Where(v => v.PollId == newVote.PollId && v.PollOptionId == newVote.PollOptionId)
                .CountAsync();

            return new PollOptionDto
            {
                Id = newVote.PollOptionId,
                Votes = votes,
                HasVoted = hasVoted // Set HasVoted based on whether the user has voted or not
            };
        }
    }
}