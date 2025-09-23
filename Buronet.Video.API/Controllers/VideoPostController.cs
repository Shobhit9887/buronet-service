using Buronet.Video.API.Models;
using Buronet.Video.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Buronet.Video.API.Controllers;

[ApiController]
[Route("api/videoposts")]
public class VideoPostsController : ControllerBase
{
    private readonly VideoPostService _videoPostService;
    private readonly List<string> _allowedVideoTypes = new() { "video/mp4", "video/quicktime", "video/webm" };
    private const long _maxFileSize = 500 * 1024 * 1024; // 500 MB

    public VideoPostsController(VideoPostService videoPostService)
    {
        _videoPostService = videoPostService;
    }

    [HttpGet]
    public async Task<List<VideoPost>> Get() =>
        await _videoPostService.GetAsync();

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateVideoPost([FromForm] CreateVideoPostRequest request)
    {
        if (request.VideoFile == null || request.VideoFile.Length == 0)
            return BadRequest("A video file is required.");
        if (request.VideoFile.Length > _maxFileSize)
            return BadRequest($"File size exceeds the limit of {_maxFileSize / 1024 / 1024} MB.");
        if (!_allowedVideoTypes.Contains(request.VideoFile.ContentType.ToLower()))
            return BadRequest($"Invalid file format. Allowed formats are: {string.Join(", ", _allowedVideoTypes)}");

        // IMPORTANT: Replace this with your actual cloud storage upload logic
        var placeholderMediaUrl = $"https://cdn.buronet.example/videos/{Guid.NewGuid()}_{request.VideoFile.FileName}";
        var placeholderThumbnailUrl = $"https://cdn.buronet.example/thumbnails/{Guid.NewGuid()}.jpg";

        var newPost = new VideoPost
        {
            Creator = new Creator { Id = request.CreatorId, Handle = request.CreatorHandle, Name = request.CreatorName, Pic = request.CreatorPic },
            Submission = new Submission { Title = request.Title, Description = request.Description, MediaUrl = placeholderMediaUrl, Thumbnail = placeholderThumbnailUrl },
            Comment = new Comment(),
            Reaction = new Reaction()
        };

        await _videoPostService.CreateAsync(newPost);
        return CreatedAtAction(nameof(Get), new { id = newPost.PostId }, newPost);
    }
}
