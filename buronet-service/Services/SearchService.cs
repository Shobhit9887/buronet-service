using buronet_service.Data;
using buronet_service.Models.DTOs.Search;
using buronet_service.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace buronet_service.Services
{
    public class SearchService : ISearchService
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public SearchService(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SearchResultDto> UnifiedSearchAsync(string query, Guid currentUserId)
        {
            // Normalize query for case-insensitive search
            string normalizedQuery = query.ToLower();

            // 1. Concurrent Search Execution
            var userSearchTask = await SearchUsersAsync(normalizedQuery, currentUserId);
            var jobSearchTask = await _fetchJobsFromExternalDatabase(normalizedQuery);
            var examSearchTask = await _fetchExamsFromExternalDatabase(normalizedQuery);

            // Wait for both searches to complete
            //await Task.WhenAll(userSearchTask, jobSearchTask);

            // 2. Combine and Aggregate Results
            var allResults = new SearchResultDto
            {
                Results = new List<UnifiedSearchResultItem>(),
                TotalUserCount = userSearchTask.Count,
                TotalJobCount = jobSearchTask.Count,
                TotalExamCount = examSearchTask.Count
            };

            // Add User Results
            allResults.Results.AddRange(userSearchTask);

            // Add Job Results
            allResults.Results.AddRange(jobSearchTask);
            allResults.Results.AddRange(examSearchTask);

            // Optional: Implement complex ranking/sorting logic here if needed
            // For now, we'll sort alphabetically by title for simplicity
            allResults.Results = allResults.Results.OrderBy(r => r.Title).ToList();

            return allResults;
        }

        // --- Data Source 1: Internal User Search (MySQL) ---
        private async Task<List<UnifiedSearchResultItem>> SearchUsersAsync(string normalizedQuery, Guid currentUserId)
        {
            // Search logic (name, headline, skills, company)
            var matchingUsers = await _context.UserProfiles
                .Where(u => u.Id != currentUserId && // Exclude current user
                            (u.FirstName.ToLower().Contains(normalizedQuery) ||
                             u.LastName.ToLower().Contains(normalizedQuery) ||
                             u.Headline.ToLower().Contains(normalizedQuery)))
                .Take(50) // Limit to a reasonable number of results
                .ToListAsync();

            // Map to Unified DTO
            return matchingUsers.Select(u => new UnifiedSearchResultItem
            {
                Id = u.Id.ToString(),
                Type = "User",
                Title = $"{u.FirstName} {u.LastName}",
                Subtitle = u.Headline,
                LinkUrl = $"/profile/{u.Id}",
                Payload = new UserSearchResultPayload
                {
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    CurrentPosition = (u.Experiences.Count > 0 ? u.Experiences.OrderBy(e => e.StartDate).First().Title : u.Headline),
                    Location = u.City
                }
            }).ToList();
        }

        // --- Data Source 2: External Job Database (Mocked Integration) ---
        // **Note: In a real microservice, this would be an HTTP call to a separate JobService API.**
        private async Task<List<UnifiedSearchResultItem>> _fetchJobsFromExternalDatabase(string normalizedQuery)
        {
            // 1. Get the configured HttpClient
            var client = _httpClientFactory.CreateClient("JobService");

            // 2. Construct the request URL
            // The JobService endpoint should be a GET endpoint that accepts a 'q' query parameter
            var requestUri = $"/api/jobs/search?keyword={Uri.EscapeDataString(normalizedQuery)}";

            try
            {
                // 3. Make the HTTP GET call
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    // 4. Read content as a stream for performance
                    using var contentStream = await response.Content.ReadAsStreamAsync();

                    // 5. Deserialize the JSON response
                    // Use lenient deserialization settings
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var jobServiceResponse = await JsonSerializer.DeserializeAsync<JobServiceSearchResponse>(
                        contentStream,
                        options
                    );

                    List<UnifiedSearchResultItem> results = new List<UnifiedSearchResultItem>();

                    foreach (Job job in jobServiceResponse?.data)
                    {
                        UnifiedSearchResultItem item = new UnifiedSearchResultItem
                        {
                            Id = job.Id,
                            Type = "Job",
                            Title = job.JobTitle,
                            Subtitle = "",
                            LinkUrl = "/jobs/" + job.Id,
                            Payload = new JobSearchResultPayload
                            {
                                CompanyName = job.CompanyName,
                                Location = job.Location,
                                JobType = job.EmploymentType,
                                ApplyLink = job.ApplyLink?.Link
                            }
                        };
                        results.Add(item);
                    }

                    // Return the list of results, or an empty list if deserialization fails/data is null
                    //return jobServiceResponse?.data ?? new List<Job>();
                    return results;
                }
                else
                {
                    // Log the error for debugging purposes in a real application
                    // _logger.LogError($"JobService search failed with status: {response.StatusCode}");
                    return new List<UnifiedSearchResultItem>();
                }
            }
            catch (HttpRequestException ex)
            {
                // Log connectivity errors
                // _logger.LogError(ex, "HTTP call to JobService failed.");
                return new List<UnifiedSearchResultItem>();
            }
        }

        private async Task<List<UnifiedSearchResultItem>> _fetchExamsFromExternalDatabase(string normalizedQuery)
        {
            // 1. Get the configured HttpClient
            var client = _httpClientFactory.CreateClient("JobService");

            // 2. Construct the request URL
            // The JobService endpoint should be a GET endpoint that accepts a 'q' query parameter
            var requestUri = $"/api/exams/search?keyword={Uri.EscapeDataString(normalizedQuery)}";

            try
            {
                // 3. Make the HTTP GET call
                var response = await client.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    // 4. Read content as a stream for performance
                    using var contentStream = await response.Content.ReadAsStreamAsync();

                    // 5. Deserialize the JSON response
                    // Use lenient deserialization settings
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var examServiceResponse = await JsonSerializer.DeserializeAsync<ExamServiceSearchResponse>(
                        contentStream,
                        options
                    );

                    List<UnifiedSearchResultItem> results = new List<UnifiedSearchResultItem>();

                    foreach (Exam exam in examServiceResponse?.data)
                    {
                        UnifiedSearchResultItem item = new UnifiedSearchResultItem
                        {
                            Id = exam.Id,
                            Type = "Exam",
                            Title = exam.ExamTitle,
                            Subtitle = exam.ExamSummary,
                            LinkUrl = "/exams/" + exam.Id,
                            Payload = new ExamSearchResultPayload
                            {
                                ConductingBody = exam.ConductingBody,
                                ReferenceNumber = exam.ReferenceNumber,
                                ExamSummary = exam.ExamSummary
                            }
                        };
                        results.Add(item);
                    }

                    // Return the list of results, or an empty list if deserialization fails/data is null
                    //return jobServiceResponse?.data ?? new List<Job>();
                    return results;
                }
                else
                {
                    // Log the error for debugging purposes in a real application
                    // _logger.LogError($"JobService search failed with status: {response.StatusCode}");
                    return new List<UnifiedSearchResultItem>();
                }
            }
            catch (HttpRequestException ex)
            {
                // Log connectivity errors
                // _logger.LogError(ex, "HTTP call to JobService failed.");
                return new List<UnifiedSearchResultItem>();
            }
        }
    }
}