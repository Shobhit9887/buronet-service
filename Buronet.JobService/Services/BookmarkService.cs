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

    public async Task<List<UserJobBookmark>> GetJobBookmarksByUserIdAsync(string userId)
    {
        return await _context.UserJobBookmarks
            .Where(b => b.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserExamBookmark>> GetExamBookmarksByUserIdAsync(string userId)
    {
        return await _context.UserExamBookmarks
            .Where(b => b.UserId == userId)
            .ToListAsync();
    }

    public async Task CreateAsync(string userId, string Id, string bookmarkType)
    {
        if (bookmarkType == "job")
        {
            var existingBookmark = await _context.UserJobBookmarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.JobId == Id);

            if (existingBookmark == null)
            {
                var newBookmark = new UserJobBookmark { UserId = userId, JobId = Id };
                _context.UserJobBookmarks.Add(newBookmark);
                await _context.SaveChangesAsync();
            }
        } else if (bookmarkType == "exam")
        {
            var existingBookmark = await _context.UserExamBookmarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.ExamId == Id);

            if (existingBookmark == null)
            {
                var newBookmark = new UserExamBookmark { UserId = userId, ExamId = Id };
                _context.UserExamBookmarks.Add(newBookmark);
                await _context.SaveChangesAsync();
            }
        }
    }

    public async Task RemoveAsync(string userId, string Id, string bookmarkType)
    {
        if (bookmarkType == "job")
        {
            var bookmarkToRemove = await _context.UserJobBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.JobId == Id);

            if (bookmarkToRemove != null)
            {
                _context.UserJobBookmarks.Remove(bookmarkToRemove);
                await _context.SaveChangesAsync();
            }
        } else if (bookmarkType == "exam")
        {
            var bookmarkToRemove = await _context.UserExamBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.ExamId == Id);

            if (bookmarkToRemove != null)
            {
                _context.UserExamBookmarks.Remove(bookmarkToRemove);
                await _context.SaveChangesAsync();
            }
        }
        
    }
}
