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
        private readonly HttpClient _httpClient;

        public CloudinaryController(ICloudinaryService cloudinaryService, IConfiguration configuration, HttpClient httpClient)
        {
            _cloudinaryService = cloudinaryService;
            _configuration = configuration;
            _httpClient = httpClient;
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

        /// <summary>
        /// Deletes assets from Cloudinary by their URLs and removes the byte from MongoDB if it's a byte post.
        /// </summary>
        [HttpPost("delete-assets")]
        public async Task<IActionResult> DeleteAssets([FromBody] DeleteAssetsRequestDto request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            try
            {
                var deletedCount = 0;

                // Delete media URL if provided
                if (!string.IsNullOrEmpty(request.url))
                {
                    var publicId = ExtractPublicIdFromUrl(request.url);
                    var resourceType = ExtractResourceTypeFromUrl(request.url);
                    System.Diagnostics.Debug.WriteLine($"Extracted public ID from media URL: '{publicId}' (type: {resourceType}) from URL: '{request.url}'");
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        var deleted = await _cloudinaryService.DeleteAssetAsync(publicId, resourceType);
                        if (deleted) deletedCount++;
                    }

                    // Delete byte from MongoDB if it's a byte post
                    await DeleteByteFromMongoDbAsync(request.url);
                }

                // Delete thumbnail URL if provided
                if (!string.IsNullOrEmpty(request.ThumbnailUrl))
                {
                    var publicId = ExtractPublicIdFromUrl(request.ThumbnailUrl);
                    var resourceType = ExtractResourceTypeFromUrl(request.ThumbnailUrl);
                    System.Diagnostics.Debug.WriteLine($"Extracted public ID from thumbnail URL: '{publicId}' (type: {resourceType}) from URL: '{request.ThumbnailUrl}'");
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        var deleted = await _cloudinaryService.DeleteAssetAsync(publicId, resourceType);
                        if (deleted) deletedCount++;
                    }
                }

                return Ok(new { message = $"Successfully deleted {deletedCount} asset(s)." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete assets.", details = ex.Message });
            }
        }

        private async Task DeleteByteFromMongoDbAsync(string mediaUrl)
        {
            try
            {
                var bytesApiUrl = _configuration["ServiceUrls:BytesApi"];
                if (string.IsNullOrEmpty(bytesApiUrl))
                    return;

                var deleteUrl = $"{bytesApiUrl}/api/bytes/delete-by-media-url";
                var request = new { mediaUrl = mediaUrl };
                
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(request),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(deleteUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete byte from MongoDB: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting byte from MongoDB: {ex.Message}");
                // Don't throw - Cloudinary deletion succeeded, MongoDB deletion is secondary
            }
        }

        private string ExtractPublicIdFromUrl(string cloudinaryUrl)
        {
            if (string.IsNullOrEmpty(cloudinaryUrl))
                return null;

            try
            {
                // Match pattern: /upload/v{version}/ followed by public_id with optional extension
                var match = System.Text.RegularExpressions.Regex.Match(cloudinaryUrl, @"/upload/(?:v\d+/)?([^/?]+)");
                if (match.Success)
                {
                    var fullId = match.Groups[1].Value;
                    // Remove file extension if present
                    return System.IO.Path.GetFileNameWithoutExtension(fullId);
                }

                // Fallback: extract filename from URL
                var uri = new Uri(cloudinaryUrl);
                var path = uri.AbsolutePath;
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                return fileName;
            }
            catch
            {
                return null;
            }
        }

        private string ExtractResourceTypeFromUrl(string cloudinaryUrl)
        {
            if (string.IsNullOrEmpty(cloudinaryUrl))
                return "image"; // default

            try
            {
                // Check if URL contains /video/ or /image/
                if (cloudinaryUrl.Contains("/video/"))
                    return "video";
                if (cloudinaryUrl.Contains("/image/"))
                    return "image";
                
                // Default to image
                return "image";
            }
            catch
            {
                return "image";
            }
        }
    }

    public class SignatureRequestDto
    {
        public string? ResourceType { get; set; }
    }

    public class DeleteAssetsRequestDto
    {
        public string? url { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
