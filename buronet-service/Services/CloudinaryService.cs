using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace buronet_service.Services
{
    public interface ICloudinaryService
    {
        string GenerateSignature(Dictionary<string, string> parameters);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly string _apiSecret;

        public CloudinaryService(IConfiguration configuration)
        {
            _apiSecret = configuration["Cloudinary:ApiSecret"] 
                ?? throw new InvalidOperationException("Cloudinary:ApiSecret is not configured");
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
    }
}
