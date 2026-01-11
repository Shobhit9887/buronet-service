using Buronet.Bytes.API.Models;
using Buronet.Bytes.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Buronet.Bytes.API.Controllers;

[ApiController]
[Route("api/bytes")] // The main route for this controller
public class BytesController : ControllerBase
{
    private readonly BytePostService _bytePostService;
    private readonly List<string> _allowedVideoTypes = new() { "video/mp4", "video/quicktime", "video/webm" };
    private const long _maxFileSize = 500 * 1024 * 1024; // 500 MB

    public BytesController(BytePostService bytePostService)
    {
        _bytePostService = bytePostService;
    }

    [HttpGet]
    public async Task<List<BytePost>> Get() =>
        await _bytePostService.GetAsync();

    [HttpPost("upload")] // Route is POST /api/bytes/upload
    // [Authorize] // TODO: Uncomment this once your authentication is configured
    public async Task<IActionResult> UploadByte([FromBody] CreateByteRequest request)
    {
        if (request.MediaUrl == null || request.MediaUrl.Length == 0)
            return BadRequest("A video file is required.");
        //if (request.ByteFile.Length > _maxFileSize)?
            //return BadRequest($"File size exceeds the limit of {_maxFileSize / 1024 / 1024} MB.");
        //if (!_allowedVideoTypes.Contains(request.ByteFile.ContentType.ToLower()))
            //return BadRequest($"Invalid file format. Allowed formats are: {string.Join(", ", _allowedVideoTypes)}");

        // --- Get User Info from Auth Token (Secure) ---
        var creatorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "e70c3bbb-eec9-4a44-82ca-d69d5ce2a213"; // Fallback for testing
        var creatorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "soniya";
        var creatorPic = request.CreatorPic;

        if (string.IsNullOrEmpty(creatorId) || string.IsNullOrEmpty(creatorName))
            return Unauthorized("User identity could not be determined from token.");

        // --- Upload to Cloud & Get URL ---
        // TODO: Replace this with your actual cloud storage upload logic (e.g., to AWS S3)
        var mediaUrl = request.MediaUrl;
        var thumbnailUrl = request.Thumbnail;

        // --- Create DB Document ---
        var newPost = new BytePost
        {
            Creator = new Creator { Id = creatorId, Name = creatorName, Pic = creatorPic },
            Submission = new Submission { Title = request.Title, Description = request.Description, MediaUrl = mediaUrl, Thumbnail = thumbnailUrl }
        };

        await _bytePostService.CreateAsync(newPost);
        return CreatedAtAction(nameof(Get), new { id = newPost.Id }, newPost);
    }

    [HttpGet("feed")]
    //[Authorize]
    public async Task<IActionResult> GetFeed([FromQuery] string filter = "For You")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        switch (filter)
        {
            case "Connections":
                var mockConnectionIds = new List<string> { "mock-user-id-2", "mock-user-id-3" };
                var connectionFeed = await _bytePostService.GetConnectionsFeedAsync(mockConnectionIds);
                return Ok(connectionFeed);

            case "Popular":
                var popularFeed = await _bytePostService.GetPopularFeedAsync();
                return Ok(popularFeed);

            case "For You":
            default:
                var forYouFeed = await _bytePostService.GetForYouFeedAsync(userId);
                return Ok(forYouFeed);
        }
    }


}
