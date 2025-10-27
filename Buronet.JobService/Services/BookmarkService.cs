using Buronet.JobService.Data;
using Buronet.JobService.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace Buronet.JobService.Services;

/// <summary>
/// Implements the business logic for managing bookmarks using EF Core and MySQL.
/// </summary>
public class BookmarkService : IBookmarkService
{
    private readonly JobDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BookmarkService(JobDbContext context, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task SendNotificationToService(Guid userId, string title, string message, string type, string redirectUrl, string? targetId = null)
    {
        // Implementation of this method goes here (using IHttpClientFactory)
        //string? userToken = await _httpContextAccessor.HttpContext!.GetTokenAsync("access_token");
        var client = _httpClientFactory.CreateClient("NotificationService");
        //if (!string.IsNullOrEmpty(userToken))
        //{
        //    client.DefaultRequestHeaders.Authorization =
        //        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
        //}
        var notificationPayload = new { UserId = userId, Title = title, Message = message, Type = type, RedirectUrl = redirectUrl, TargetId = targetId };

        try
        {
            await client.PostAsJsonAsync("/api/notifications/internal-create", notificationPayload);
        }
        catch (Exception ex)
        {
            // Log error
        }
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

                await SendNotificationToService(
                userId: Guid.Parse(userId),
                title: "Job Bookmarked",
                message: $"You saved a job to your list.",
                type: "JobBookmarkAdded",
                redirectUrl: $"/jobs/{Id}",
                targetId: Id
            );
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


                await SendNotificationToService(
                    userId: Guid.Parse(userId),
                    title: "Exam Bookmarked",
                    message: $"You are now tracking exam: {Id}",
                    type: "ExamBookmarkAdded",
                    redirectUrl: $"/exams/{Id}",
                    targetId: Id
                );
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
