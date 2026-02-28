using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using buronet_service.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace buronet_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CloudinaryController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IConfiguration _configuration;

        public CloudinaryController(ICloudinaryService cloudinaryService, IConfiguration configuration)
        {
            _cloudinaryService = cloudinaryService;
            _configuration = configuration;
        }

        private Guid? GetCurrentUserId()
        {
            string? userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out Guid userIdGuid))
            {
                return userIdGuid;
            }
            return null;
        }

        /// <summary>
        /// Generates a signed upload signature for Cloudinary.
        /// Frontend calls this to get a signature before uploading to Cloudinary.
        /// </summary>
        [HttpPost("get-signature")]
        public IActionResult GetSignature([FromBody] SignatureRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue || userId.Value == Guid.Empty)
                return Unauthorized("User not authenticated.");

            if (request == null)
                return BadRequest("Request body is required.");

            try
            {
                var cloudName = _configuration["Cloudinary:CloudName"];
                var apiKey = _configuration["Cloudinary:ApiKey"];
                
                // Determine upload preset based on resource type
                var uploadPreset = request?.ResourceType == "video" 
                    ? _configuration["Cloudinary:VideoPreset"] 
                    : _configuration["Cloudinary:ImagePreset"];

                // Generate timestamp on backend
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var publicId = $"post-{userId}-{timestamp}";
                var folder = "posts";

                // Create parameters for signature generation
                // Only include parameters that Cloudinary will validate
                var parameters = new Dictionary<string, string>
                {
                    { "timestamp", timestamp.ToString() },
                    { "upload_preset", uploadPreset },
                    { "public_id", publicId }
                };

                // Generate signature
                var signature = _cloudinaryService.GenerateSignature(parameters);

                return Ok(new
                {
                    signature = signature,
                    timestamp = timestamp,
                    uploadPreset = uploadPreset,
                    cloudName = cloudName,
                    apiKey = apiKey,
                    publicId = publicId,
                    folder = folder
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to generate signature.", details = ex.Message });
            }
        }
    }

    public class SignatureRequestDto
    {
        public string? ResourceType { get; set; }
    }
}
