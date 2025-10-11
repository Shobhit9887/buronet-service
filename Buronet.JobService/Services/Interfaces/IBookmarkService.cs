using Buronet.JobService.Models;

namespace Buronet.JobService.Services;

/// <summary>
/// Defines the contract for the bookmark service.
/// </summary>
public interface IBookmarkService
{
    Task<List<UserJobBookmark>> GetJobBookmarksByUserIdAsync(string userId);
    Task<List<UserExamBookmark>> GetExamBookmarksByUserIdAsync(string userId);
    Task CreateAsync(string userId, string Id, string bookmarkType);
    Task RemoveAsync(string userId, string Id, string bookmarkType);
}
