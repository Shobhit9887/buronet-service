using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace buronet_service.Storage
{
    public class CloudinaryBlobStorage : IBlobStorage
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _imageUploadPreset;
        private readonly string _videoUploadPreset;

        public CloudinaryBlobStorage(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName))
                throw new InvalidOperationException("Cloudinary:CloudName is missing.");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Cloudinary:ApiKey is missing.");
            if (string.IsNullOrWhiteSpace(apiSecret))
                throw new InvalidOperationException("Cloudinary:ApiSecret is missing.");

            _imageUploadPreset = configuration["Cloudinary:ImagePreset"]
                ?? throw new InvalidOperationException("Cloudinary:ImagePreset is missing.");
            _videoUploadPreset = configuration["Cloudinary:VideoPreset"]
                ?? throw new InvalidOperationException("Cloudinary:VideoPreset is missing.");

            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret))
            {
                Api = { Secure = true }
            };
        }

        public Task SaveAsync(string path, byte[] data)
            => throw new NotSupportedException("CloudinaryBlobStorage requires uploading via UploadUnsignedAsync and storing returned URL.");

        public string? GetPath(string path) => null;

        public string? GetPublicUrl(string path) => path;

        public async Task<string> UploadUnsignedAsync(byte[] data, string fileName, string? contentType)
        {
            var isVideo = !string.IsNullOrWhiteSpace(contentType) &&
                          contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

            var preset = isVideo ? _videoUploadPreset : _imageUploadPreset;

            using var ms = new MemoryStream(data);

            if (isVideo)
            {
                var uploadParams = new VideoUploadParams
                {
                    File = new FileDescription(fileName, ms),
                    UploadPreset = preset
                    // IMPORTANT: do not set PublicId here unless your unsigned preset allows it.
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.StatusCode is not System.Net.HttpStatusCode.OK and not System.Net.HttpStatusCode.Created)
                    throw new InvalidOperationException($"Cloudinary upload failed: {result.Error?.Message}");

                return result.SecureUrl?.ToString()
                       ?? throw new InvalidOperationException("Cloudinary response didn't include SecureUrl.");
            }
            else
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, ms),
                    UploadPreset = preset
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.StatusCode is not System.Net.HttpStatusCode.OK and not System.Net.HttpStatusCode.Created)
                    throw new InvalidOperationException($"Cloudinary upload failed: {result.Error?.Message}");

                return result.SecureUrl?.ToString()
                       ?? throw new InvalidOperationException("Cloudinary response didn't include SecureUrl.");
            }
        }
    }
}