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
            string normalizedQuery = query.ToLower();

            // 1. Start the Tasks (Do not await them yet)
            var userSearchTask = SearchUsersAsync(normalizedQuery, currentUserId);
            var jobSearchTask = _fetchJobsFromExternalDatabase(normalizedQuery);
            var examSearchTask = _fetchExamsFromExternalDatabase(normalizedQuery);

            // 2. Wait for ALL of them to complete
            await Task.WhenAll(userSearchTask, jobSearchTask, examSearchTask);

            // 3. Extract the actual Lists from the Tasks
            var userResults = await userSearchTask; // Or userSearchTask.Result
            var jobResults = await jobSearchTask;
            var examResults = await examSearchTask;

            // 4. Combine and Aggregate Results
            var allResults = new SearchResultDto
            {
                Results = new List<UnifiedSearchResultItem>(),
                TotalUserCount = userResults.Count,  // Now .Count works!
                TotalJobCount = jobResults.Count,
                TotalExamCount = examResults.Count
            };

            // Add Results to the list
            allResults.Results.AddRange(userResults);
            allResults.Results.AddRange(jobResults);
            allResults.Results.AddRange(examResults);

            // Sort and return
            allResults.Results = allResults.Results.OrderBy(r => r.Title).ToList();

            return allResults;
        }

        // --- Data Source 1: Internal User Search (MySQL) ---
        private async Task<List<UnifiedSearchResultItem>> SearchUsersAsync(string normalizedQuery, Guid currentUserId)
        {
            // Split the query into individual words
            var words = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var queryable = _context.UserProfiles.AsQueryable();

            // Exclude current user
            queryable = queryable.Where(u => u.Id != currentUserId);

            // Filter by words
            foreach (var word in words)
            {
                // This ensures every word in the search appears SOMEWHERE in the name or headline
                queryable = queryable.Where(u =>
                    u.FirstName.ToLower().Contains(word) ||
                    u.LastName.ToLower().Contains(word) ||
                    u.Headline.ToLower().Contains(word));
            }

            var matchingUsers = await queryable.Take(50).ToListAsync();

            return matchingUsers.Select(u => new UnifiedSearchResultItem
            {
                Id = u.Id.ToString(),
                Type = "User",
                Title = $"{u.FirstName} {u.LastName}",
                Subtitle = u.Headline,
                LinkUrl = $"/profile/{u.Id}",
                Payload = new UserSearchResultPayload
                {
                    // Note: Changed First() to FirstOrDefault() to prevent crashes if Experiences is empty
                    CurrentPosition = u.Experiences.OrderBy(e => e.StartDate).FirstOrDefault()?.Title ?? u.Headline,
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