using Buronet.JobService.Models;

namespace Buronet.JobService.Services;

/// <summary>
/// Defines the contract for the bookmark service.
/// </summary>
public interface IBookmarkService
{
    Task<List<UserJobBookmark>> GetByUserIdAsync(string userId);
    Task CreateAsync(string userId, string jobId);
    Task RemoveAsync(string userId, string jobId);
}
