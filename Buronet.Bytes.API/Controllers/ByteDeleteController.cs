using Buronet.Bytes.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Buronet.Bytes.API.Controllers
{
    [ApiController]
    [Route("api/bytes")]
    public class ByteDeleteController : ControllerBase
    {
        private readonly BytePostService _bytePostService;

        public ByteDeleteController(BytePostService bytePostService)
        {
            _bytePostService = bytePostService;
        }

        [HttpPost("delete-by-media-url")]
        public async Task<IActionResult> DeleteByMediaUrl([FromBody] DeleteByteRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MediaUrl))
                return BadRequest("MediaUrl is required.");

            try
            {
                await _bytePostService.DeleteByteByMediaUrlAsync(request.MediaUrl);
                return Ok(new { message = "Byte deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete byte.", details = ex.Message });
            }
        }
    }

    public class DeleteByteRequest
    {
        public string MediaUrl { get; set; } = string.Empty;
    }
}
