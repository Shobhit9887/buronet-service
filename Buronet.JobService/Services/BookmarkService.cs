using Microsoft.EntityFrameworkCore;
using Buronet.JobService.Data;
using Buronet.JobService.Models;

namespace Buronet.JobService.Services;

/// <summary>
/// Implements the business logic for managing bookmarks using EF Core and MySQL.
/// </summary>
public class BookmarkService : IBookmarkService
{
    private readonly JobDbContext _context;

    public BookmarkService(JobDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserJobBookmark>> GetByUserIdAsync(string userId)
    {
        return await _context.UserJobBookmarks
            .Where(b => b.UserId == userId)
            .ToListAsync();
    }

    public async Task CreateAsync(string userId, string jobId)
    {
        var existingBookmark = await _context.UserJobBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.JobId == jobId);

        if (existingBookmark == null)
        {
            var newBookmark = new UserJobBookmark { UserId = userId, JobId = jobId };
            _context.UserJobBookmarks.Add(newBookmark);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveAsync(string userId, string jobId)
    {
        var bookmarkToRemove = await _context.UserJobBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.JobId == jobId);

        if (bookmarkToRemove != null)
        {
            _context.UserJobBookmarks.Remove(bookmarkToRemove);
            await _context.SaveChangesAsync();
        }
    }
}
