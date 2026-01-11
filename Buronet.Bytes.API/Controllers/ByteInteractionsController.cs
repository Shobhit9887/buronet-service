using Buronet.Bytes.API.Models;
using Buronet.Bytes.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Buronet.Bytes.API.Controllers
{
    [ApiController]
    [Route("api/bytes/{byteId}/[action]")]
    [Authorize]
    public class ByteInteractionsController : ControllerBase
    {
        private readonly Services.BytePostService _bytePostService;

        public ByteInteractionsController(Services.BytePostService bytePostService)
        {
            _bytePostService = bytePostService;
        }

        [HttpPost]
        public async Task<IActionResult> Like(string byteId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _bytePostService.ToggleLikeAsync(byteId, userId);
            return Ok(new { message = "Like toggled successfully." });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Comments(string byteId)
        {
            var comments = await _bytePostService.GetCommentsAsync(byteId);
            return Ok(comments);
        }

        [HttpPost]
        public async Task<IActionResult> Comments(string byteId, [FromBody] Models.CreateCommentRequest request)
        {
            var creator = new Models.Creator
            {
                Id = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Name = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown User",
                Pic = User.FindFirstValue("picture") ?? ""
            };

            var newComment = new Models.Comment
            {
                ByteId = byteId,
                Creator = creator,
                Text = request.Text
            };

            await _bytePostService.AddCommentAsync(newComment);
            return CreatedAtAction(nameof(Comments), new { byteId = byteId }, newComment);
        }
    }
}
