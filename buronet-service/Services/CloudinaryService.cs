using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace buronet_service.Services
{
    public interface ICloudinaryService
    {
        string GenerateSignature(Dictionary<string, string> parameters);
        Task<bool> DeleteAssetAsync(string publicId, string resourceType = "image");
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly string _apiSecret;
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            _apiSecret = configuration["Cloudinary:ApiSecret"] 
                ?? throw new InvalidOperationException("Cloudinary:ApiSecret is not configured");
            
            var cloudName = configuration["Cloudinary:CloudName"]
                ?? throw new InvalidOperationException("Cloudinary:CloudName is not configured");
            var apiKey = configuration["Cloudinary:ApiKey"]
                ?? throw new InvalidOperationException("Cloudinary:ApiKey is not configured");
            
            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, _apiSecret));
        }

        /// <summary>
        /// Generates a signed upload signature for Cloudinary.
        /// </summary>
        /// <param name="parameters">Dictionary of parameters to sign (e.g., timestamp, upload_preset, etc.)</param>
        /// <returns>SHA-256 signature as hex string</returns>
        public string GenerateSignature(Dictionary<string, string> parameters)
        {
            // Filter out null or empty values
            var filteredParams = new Dictionary<string, string>();
            foreach (var kvp in parameters)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    filteredParams[kvp.Key] = kvp.Value;
                }
            }

            // Sort parameters alphabetically and create query string
            var sortedParams = new SortedDictionary<string, string>(filteredParams);
            var queryString = new StringBuilder();

            foreach (var kvp in sortedParams)
            {
                if (queryString.Length > 0)
                    queryString.Append("&");
                queryString.Append($"{kvp.Key}={kvp.Value}");
            }

            // Append API secret
            queryString.Append(_apiSecret);

            // Generate SHA-256 hash
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(queryString.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Deletes an asset from Cloudinary by public ID.
        /// </summary>
        /// <param name="publicId">The public ID of the asset to delete</param>
        /// <param name="resourceType">The resource type (video or image)</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public async Task<bool> DeleteAssetAsync(string publicId, string resourceType = "image")
        {
            try
            {
                // Convert string to ResourceType enum
                var type = resourceType.ToLower() == "video" ? ResourceType.Video : ResourceType.Image;
                
                var deleteParams = new DeletionParams(publicId) { ResourceType = type };
                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                System.Diagnostics.Debug.WriteLine($"Cloudinary delete result ({resourceType}) for '{publicId}': {result.Result}");
                
                if (result.Result == "ok")
                    return true;

                // If not found with specified type, try the other type
                if (result.Result == "not_found")
                {
                    var otherType = type == ResourceType.Video ? ResourceType.Image : ResourceType.Video;
                    deleteParams = new DeletionParams(publicId) { ResourceType = otherType };
                    result = await _cloudinary.DestroyAsync(deleteParams);
                    System.Diagnostics.Debug.WriteLine($"Cloudinary delete result ({otherType}) for '{publicId}': {result.Result}");
                }
                
                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting asset from Cloudinary: {ex.Message}");
                return false;
            }
        }
    }
}
