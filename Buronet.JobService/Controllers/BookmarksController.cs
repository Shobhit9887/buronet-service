using Microsoft.AspNetCore.Mvc;
using Buronet.JobService.Services;

namespace Buronet.JobService.Controllers;

/// <summary>
/// API controller for managing user job bookmarks.
/// </summary>
[ApiController]
[Route("api/bookmarks/{userId}")]
public class BookmarksController : ControllerBase
{
    private readonly IBookmarkService _bookmarkService;

    public BookmarksController(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobBookmarks(string userId)
    {
        var bookmarks = await _bookmarkService.GetJobBookmarksByUserIdAsync(userId);
        return Ok(bookmarks);
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExamBookmarks(string userId)
    {
        var bookmarks = await _bookmarkService.GetExamBookmarksByUserIdAsync(userId);
        return Ok(bookmarks);
    }

    [HttpPost("job")]
    public async Task<IActionResult> AddJobBookmark(string userId, [FromBody] AddBookmarkRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _bookmarkService.CreateAsync(userId, request.Id, "job");
        return CreatedAtAction(nameof(GetJobBookmarks), new { userId }, null);
    }

    [HttpDelete("job/{jobId}")]
    public async Task<IActionResult> RemoveJobBookmark(string userId, string jobId)
    {
        await _bookmarkService.RemoveAsync(userId, jobId, "job");
        return NoContent();
    }

    [HttpPost("exam")]
    public async Task<IActionResult> AddExamBookmark(string userId, [FromBody] AddBookmarkRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _bookmarkService.CreateAsync(userId, request.Id, "exam");
        return CreatedAtAction(nameof(GetExamBookmarks), new { userId }, null);
    }

    [HttpDelete("exam/{examId}")]
    public async Task<IActionResult> RemoveExamBookmark(string userId, string examId)
    {
        await _bookmarkService.RemoveAsync(userId, examId, "exam");
        return NoContent();
    }
}
