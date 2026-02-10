using System.Net.Http.Json;

namespace buronet_service.Storage
{
    public class CloudinaryBlobStorage : IBlobStorage
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _imageUploadUrl;
        private readonly string _videoUploadUrl;
        private readonly string _imageUploadPreset;
        private readonly string _videoUploadPreset;

        public CloudinaryBlobStorage(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;

            var cloudName = configuration["Cloudinary:CloudName"];
            if (string.IsNullOrWhiteSpace(cloudName))
            {
                throw new InvalidOperationException("Cloudinary:CloudName is missing.");
            }

            _imageUploadPreset = configuration["Cloudinary:ImagePreset"]
                ?? throw new InvalidOperationException("Cloudinary:ImagePreset is missing.");
            _videoUploadPreset = configuration["Cloudinary:VideoPreset"]
                ?? throw new InvalidOperationException("Cloudinary:VideoPreset is missing.");

            var imageUrlTemplate = configuration["Cloudinary:ImageUrlTemplate"]
                ?? throw new InvalidOperationException("Cloudinary:ImageUrlTemplate is missing.");
            var videoUrlTemplate = configuration["Cloudinary:VideoUrlTemplate"]
                ?? throw new InvalidOperationException("Cloudinary:VideoUrlTemplate is missing.");

            _imageUploadUrl = imageUrlTemplate.Replace("{cloudName}", cloudName, StringComparison.Ordinal);
            _videoUploadUrl = videoUrlTemplate.Replace("{cloudName}", cloudName, StringComparison.Ordinal);
        }

        public async Task SaveAsync(string path, byte[] data)
        {
            // Not used: for Cloudinary we upload and return a public URL via GetPublicUrl() pattern.
            // This interface method is retained for compatibility, but use UploadAndGetUrlAsync flow in MediaService.
            throw new NotSupportedException("CloudinaryBlobStorage requires uploading via UploadAsync and storing returned URL.");
        }

        public string? GetPath(string path) => null;

        public string? GetPublicUrl(string path)
            => path;

        public async Task<string> UploadAsync(byte[] data, string fileName, string? contentType, string publicId)
        {
            var (uploadUrl, uploadPreset) = ResolveUploadTarget(contentType);

            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(uploadPreset), "upload_preset");
            //content.Add(new StringContent(publicId), "public_id");

            var fileContent = new ByteArrayContent(data);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }

            content.Add(fileContent, "file", fileName);

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = null;
            using var response = await http.PostAsync(uploadUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                // LOG THIS errorBody. It will say exactly why Cloudinary is mad.
                throw new HttpRequestException($"Cloudinary Error: {response.StatusCode} - {errorBody}");
            }
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<CloudinaryUploadResponse>();
            if (payload?.SecureUrl == null)
            {
                throw new InvalidOperationException("Cloudinary response didn't include secure_url.");
            }

            return payload.SecureUrl;
        }

        private (string uploadUrl, string uploadPreset) ResolveUploadTarget(string? contentType)
        {
            // Default to image if unknown.
            if (!string.IsNullOrWhiteSpace(contentType) &&
                contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return (_videoUploadUrl, _videoUploadPreset);
            }

            return (_imageUploadUrl, _imageUploadPreset);
        }

        private sealed class CloudinaryUploadResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("secure_url")]
            public string? SecureUrl { get; set; }

            // Cloudinary uses snake_case; System.Text.Json can bind if property matches via attribute.
            // Keep it explicit:
            //[System.Text.Json.Serialization.JsonPropertyName("secure_url")]
            //public string? SecureUrlJson { get => SecureUrl; set => SecureUrl = value; }
        }
    }
}