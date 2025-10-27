using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
using Buronet.JobService.Settings;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Buronet.JobService.Services
{
    public class UpdateFetcherService : IUpdateFetcherService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _geminiApiKey;
        private readonly string _geminiApiUrl;
        private readonly IMongoCollection<ExternalUpdate> _updatesCollection;
        private readonly ILogger<UpdateFetcherService> _logger;

        public UpdateFetcherService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IOptions<JobDBSettings> dbSettings,
            ILogger<UpdateFetcherService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("GeminiClient");
            _geminiApiKey = configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(_geminiApiKey) || _geminiApiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                //_logger.LogWarning("Gemini API Key not configured or placeholder used. Update fetching disabled.");
                _geminiApiKey = null;
            }
            _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_geminiApiKey}";
            _logger = logger;

            try
            {
                var mongoClient = new MongoClient(dbSettings.Value.ConnectionString);
                var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
                _updatesCollection = mongoDatabase.GetCollection<ExternalUpdate>(dbSettings.Value.ExternalUpdatesCollectionName);

                var indexKeysDefinition = Builders<ExternalUpdate>.IndexKeys.Ascending(u => u.ContentHash);
                var indexOptions = new CreateIndexOptions { Unique = true, Name = "contentHash_unique" };
                var indexModel = new CreateIndexModel<ExternalUpdate>(indexKeysDefinition, indexOptions);

                var existingIndexesCursor = _updatesCollection.Indexes.List();
                bool indexExists = existingIndexesCursor.ToList().Any(idx => idx["name"] == "contentHash_unique");

                if (!indexExists)
                {
                    _updatesCollection.Indexes.CreateOne(indexModel);
                    _logger.LogInformation("Created unique index on 'contentHash' for ExternalUpdates.");
                }
                else
                {
                    _logger.LogDebug("Unique index 'contentHash_unique' already exists for ExternalUpdates.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FATAL Error setting up MongoDB connection/index for ExternalUpdates.");
                throw;
            }
        }

        public async Task FetchAndStoreUpdatesAsync(string query)
        {
            if (string.IsNullOrEmpty(_geminiApiKey)) return;

            string promptText = $$$"""
                Find the 5 most recent and distinct official updates for government jobs AND competitive exams in India related to: {query}.

                You MUST use the Google Search tool to find this information.
                Focus ONLY on official government or recruitment body sources (like UPSC, SSC, IBPS, RRB, NTA websites). Do not use news or aggregator sites.

                For each update found, provide all the following details based on the 'ExternalUpdate' model schema.

                Respond with ONLY a valid JSON object in the following exact format. Do not include markdown or any other text outside the JSON structure.

                {{
                  "updates": [
                    {{
                      "title": "Title of the update",
                      "url": "https://official-source.gov.in/link-to-update",
                      "type": "Classify as 'Results', 'Admit Card', 'Exam Schedule', 'Application', or 'Other'",
                      "category": "Classify as 'Job' or 'Exam'",
                      "published_date": "YYYY-MM-DDTHH:MM:SSZ",
                      "source_name": "Official Source Name (e.g., UPSC)"
                    }}
                  ]
                }}
                """;

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = promptText } } } },
                tools = new[] { new { google_search = new { } } },
                generationConfig = new { temperature = 0.1, response_mime_type = "application/json" }
            };

            try
            {
                _logger.LogInformation("Calling Gemini API for updates query: {Query}", query);
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_geminiApiUrl, payload);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("Gemini Raw Response: {JsonResponse}", jsonResponse);

                    List<ExternalUpdateDto> parsedDtos = ParseGeminiJsonResponse(jsonResponse);

                    if (!parsedDtos.Any())
                    {
                        _logger.LogInformation("No valid updates parsed from Gemini for query: {Query}", query);
                        return;
                    }

                    int newUpdatesCount = 0;
                    int skippedCount = 0;
                    foreach (var dto in parsedDtos)
                    {
                        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Url) || dto.Url == "#")
                        {
                            _logger.LogWarning("Skipping incomplete DTO: Title='{Title}', Url='{Url}'", dto.Title, dto.Url);
                            skippedCount++;
                            continue;
                        }

                        string hash = GenerateUpdateHash(dto);
                        var filter = Builders<ExternalUpdate>.Filter.Eq(u => u.ContentHash, hash);
                        bool exists = await _updatesCollection.Find(filter).AnyAsync();

                        if (!exists)
                        {
                            var newUpdate = new ExternalUpdate
                            {
                                Title = dto.Title.Trim(),
                                Url = dto.Url.Trim(),
                                Type = InferUpdateType(dto.Title),
                                UpdateCategory = dto.UpdateCategory,
                                PublishedDate = dto.PublishedDate,
                                SourceName = dto.SourceName.Trim(),
                                ContentHash = hash,
                                FetchedDate = DateTime.UtcNow
                            };
                            await _updatesCollection.InsertOneAsync(newUpdate);
                            newUpdatesCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    _logger.LogInformation("Update fetch finished for query: '{Query}'. Inserted: {NewCount}, Skipped: {SkippedCount}", query, newUpdatesCount, skippedCount);
                }
                else
                {
                    _logger.LogError("Error from Gemini API: {StatusCode} - {ResponseContent}", response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Gemini API call/processing for query: {Query}", query);
            }
        }

        private string GenerateUpdateHash(ExternalUpdateDto dto)
        {
            string input = $"{dto.Title?.Trim().ToLowerInvariant()}|{dto.Url?.Trim().ToLowerInvariant()}";
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public async Task<List<ExternalUpdate>> GetLatestUpdatesAsync(UpdateCategory? category, int limit)
        {
            var filter = Builders<ExternalUpdate>.Filter.Empty;

            // Only filter if the category is 'Job' or 'Exam'
            if (category.HasValue && (category.Value == UpdateCategory.Job || category.Value == UpdateCategory.Exam))
            {
                filter = Builders<ExternalUpdate>.Filter.Eq(u => u.UpdateCategory, category.Value);
            }

            // Sort by the date we fetched them (most recent first)
            var sort = Builders<ExternalUpdate>.Sort.Descending(u => u.FetchedDate);

            try
            {
                return await _updatesCollection.Find(filter)
                                               .Sort(sort)
                                               .Limit(limit)
                                               .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest updates from MongoDB. Category: {Category}, Limit: {Limit}", category, limit);
                return new List<ExternalUpdate>(); // Return empty list on error
            }
        }
        private List<ExternalUpdateDto> ParseGeminiJsonResponse(string rawJsonResponse)
        {
            var updates = new List<ExternalUpdateDto>();
            try
            {
                using (JsonDocument document = JsonDocument.Parse(rawJsonResponse))
                {
                    JsonElement textElement = document.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text");
                    string jsonText = textElement.GetString() ?? "{}";
                    _logger.LogDebug("Extracted JSON text from Gemini: {JsonText}", jsonText);

                    using (JsonDocument innerDocument = JsonDocument.Parse(jsonText))
                    {
                        if (innerDocument.RootElement.TryGetProperty("updates", out var updateArray) && updateArray.ValueKind == JsonValueKind.Array)
                        {
                            _logger.LogInformation("Found {Count} potential updates in Gemini JSON.", updateArray.GetArrayLength());
                            foreach (var updateElement in updateArray.EnumerateArray())
                            {
                                try
                                {
                                    var dto = new ExternalUpdateDto();
                                    dto.Title = updateElement.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "";
                                    dto.Url = updateElement.TryGetProperty("url", out var url) ? url.GetString() ?? "#" : "#";
                                    dto.SourceName = updateElement.TryGetProperty("source_name", out var source) ? source.GetString() ?? "" : "";
                                    string categoryStr = updateElement.TryGetProperty("category", out var cat) ? cat.GetString() ?? "General" : "General";

                                    if (Enum.TryParse<UpdateCategory>(categoryStr, true, out var parsedCategory))
                                    {
                                        dto.UpdateCategory = parsedCategory;
                                    }
                                    else
                                    {
                                        dto.UpdateCategory = InferUpdateCategory(dto.Title); // Fallback
                                        _logger.LogWarning("Could not parse category '{CategoryStr}', inferred {InferredCategory} for '{Title}'", categoryStr, dto.UpdateCategory, dto.Title);
                                    }
                                    dto.Type = InferUpdateType(dto.Title); // Infer detailed type

                                    if (!string.IsNullOrWhiteSpace(dto.Title) && !string.IsNullOrWhiteSpace(dto.Url) && dto.Url != "#")
                                    {
                                        updates.Add(dto);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Skipping DTO due to missing title or valid URL: Title='{Title}', Url='{Url}'", dto.Title, dto.Url);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error parsing a single update element from Gemini JSON: {UpdateElement}", updateElement.ToString());
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Could not find 'updates' array in the JSON provided by Gemini.");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing outer JSON response structure from Gemini.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Gemini response parsing.");
            }
            return updates;
        }

        private string InferUpdateType(string text)
        {
            text = text.ToLowerInvariant();
            if (text.Contains("result")) return "Results";
            if (text.Contains("admit card") || text.Contains("hall ticket") || text.Contains("e-admit")) return "Admit Card";
            if (text.Contains("schedule") || text.Contains("date sheet") || text.Contains("exam date") || text.Contains("timetable")) return "Exam Schedule";
            if (text.Contains("apply") || text.Contains("application") || text.Contains("notification") || text.Contains("vacancy") || text.Contains("recruitment")) return "Application";
            return "Other";
        }

        private UpdateCategory InferUpdateCategory(string text)
        {
            text = text.ToLowerInvariant();
            if (text.Contains("exam") || text.Contains("cce") || text.Contains("tier") || text.Contains("prelim") || text.Contains("mains") || text.Contains("admit card") || text.Contains("result") || text.Contains("schedule") || text.Contains("syllabus") || text.Contains("upsc") || text.Contains("ssc") || text.Contains("ibps") || text.Contains("rrb") || text.Contains("nta") || text.Contains("jee") || text.Contains("neet"))
            {
                return UpdateCategory.Exam;
            }
            if (text.Contains("job") || text.Contains("recruitment") || text.Contains("vacancy") || text.Contains("post") || text.Contains("apply") || text.Contains("walk-in") || text.Contains("hiring") || text.Contains("career") || text.Contains("engineer") || text.Contains("officer") || text.Contains("manager"))
            {
                return UpdateCategory.Job;
            }
            return UpdateCategory.General;
        }
    }
}