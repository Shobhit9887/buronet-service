using Microsoft.AspNetCore.Mvc;
using Buronet.JobService.Services;

namespace Buronet.JobService.Controllers;

/// <summary>
/// API controller for managing user job bookmarks.
/// </summary>
[ApiController]
[Route("api/jobs/{userId}/bookmarks")]
public class BookmarksController : ControllerBase
{
    private readonly IBookmarkService _bookmarkService;

    public BookmarksController(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBookmarks(string userId)
    {
        var bookmarks = await _bookmarkService.GetByUserIdAsync(userId);
        return Ok(bookmarks);
    }

    [HttpPost]
    public async Task<IActionResult> AddBookmark(string userId, [FromBody] AddBookmarkRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await _bookmarkService.CreateAsync(userId, request.JobId);
        return CreatedAtAction(nameof(GetBookmarks), new { userId }, null);
    }

    [HttpDelete("{jobId}")]
    public async Task<IActionResult> RemoveBookmark(string userId, string jobId)
    {
        await _bookmarkService.RemoveAsync(userId, jobId);
        return NoContent();
    }
}
