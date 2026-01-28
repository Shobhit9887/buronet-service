using buronet_service.Services;
using buronet_service.Storage;
using Microsoft.AspNetCore.Mvc;

namespace buronet_service.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly MediaService _media;
        private readonly IBlobStorage _storage;

        public MediaController(MediaService media, IBlobStorage storage)
        {
            _media = media ?? throw new ArgumentNullException(nameof(media));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
            => Ok(new { fileId = await _media.UploadAsync(file) });

        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var media = _media.Get(id);

            var publicUrl = _storage.GetPublicUrl(media.StoragePath);
            if (!string.IsNullOrWhiteSpace(publicUrl))
            {
                return Redirect(publicUrl);
            }

            var path = _storage.GetPath(media.StoragePath);
            return PhysicalFile(path!, media.ContentType);
        }
    }
}
