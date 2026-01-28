using System.Net.Http.Json;

namespace buronet_service.Storage
{
    public class CloudinaryBlobStorage : IBlobStorage
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _uploadUrl;
        private readonly string _uploadPreset;

        public CloudinaryBlobStorage(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;

            var cloudName = configuration["Cloudinary:CloudName"];
            _uploadPreset = configuration["Cloudinary:UploadPreset"] ?? throw new InvalidOperationException("Cloudinary:UploadPreset is missing.");
            var uploadUrlTemplate = configuration["Cloudinary:UploadUrlTemplate"] ?? throw new InvalidOperationException("Cloudinary:UploadUrlTemplate is missing.");

            if (string.IsNullOrWhiteSpace(cloudName))
            {
                throw new InvalidOperationException("Cloudinary:CloudName is missing.");
            }

            _uploadUrl = uploadUrlTemplate.Replace("{cloudName}", cloudName, StringComparison.Ordinal);
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
            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(_uploadPreset), "upload_preset");
            content.Add(new StringContent(publicId), "public_id");

            var fileContent = new ByteArrayContent(data);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }

            content.Add(fileContent, "file", fileName);

            var http = _httpClientFactory.CreateClient();
            using var response = await http.PostAsync(_uploadUrl, content);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<CloudinaryUploadResponse>();
            if (payload?.SecureUrl == null)
            {
                throw new InvalidOperationException("Cloudinary response didn't include secure_url.");
            }

            return payload.SecureUrl;
        }

        private sealed class CloudinaryUploadResponse
        {
            public string? SecureUrl { get; set; }

            // Cloudinary uses snake_case; System.Text.Json can bind if property matches via attribute.
            // Keep it explicit:
            [System.Text.Json.Serialization.JsonPropertyName("secure_url")]
            public string? SecureUrlJson { get => SecureUrl; set => SecureUrl = value; }
        }
    }
}