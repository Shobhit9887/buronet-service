using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Buronet.JobService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UpdatesController : ControllerBase
    {
        private readonly IUpdateFetcherService _updateFetcherService;
        private readonly ILogger<UpdatesController> _logger;

        // DTO for the [HttpPost("fetch")] request body
        public class FetchRequestDto
        {
            [Required(ErrorMessage = "Query is required.")]
            public string Query { get; set; } = null!;
        }

        public UpdatesController(IUpdateFetcherService updateFetcherService, ILogger<UpdatesController> logger)
        {
            _updateFetcherService = updateFetcherService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the latest external updates stored in the database, optionally filtered by category.
        /// </summary>
        /// <param name="category">Optional category ("Job", "Exam"). Case-insensitive. If omitted or "General", returns all categories.</param>
        /// <param name="limit">Maximum number of updates to return (default 5).</param>
        /// <returns>A list of recent external updates.</returns>
        [HttpGet("external")]
        [ProducesResponseType(typeof(ApiResponse<List<ExternalUpdate>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetExternalUpdates([FromQuery] string? category = null, [FromQuery] int limit = 5)
        {
            UpdateCategory? categoryEnum = null;

            if (!string.IsNullOrEmpty(category))
            {
                if (Enum.TryParse<UpdateCategory>(category, true, out var parsedCategory))
                {
                    if (parsedCategory == UpdateCategory.Job || parsedCategory == UpdateCategory.Exam)
                    {
                        categoryEnum = parsedCategory;
                    }
                    else if (parsedCategory == UpdateCategory.General)
                    {
                        categoryEnum = null; // 'General' means no filter
                    }
                    else
                    {
                        _logger.LogWarning("Unexpected UpdateCategory value parsed: {ParsedCategory}. Treating as no filter.", parsedCategory);
                        categoryEnum = null;
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid category string '{Category}' received in request. Returning updates from all categories.", category);
                    categoryEnum = null; // Treat invalid string as no filter
                }
            }

            try
            {
                _logger.LogInformation("API: Request received for external updates. Category Filter: {Category}, Limit: {Limit}", categoryEnum?.ToString() ?? "All", limit);
                var updates = await _updateFetcherService.GetLatestUpdatesAsync(categoryEnum, limit);

                return Ok(new ApiResponse<List<ExternalUpdate>> { Success = true, Data = updates });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting external updates.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<List<ExternalUpdate>> { Success = false, Message = "An error occurred while fetching updates." });
            }
        }

        /// <summary>
        /// Triggers a fetch and storage of external updates from Gemini based on a query.
        /// </summary>
        /// <param name="request">The request body containing the search query.</param>
        /// <returns>A status message.</returns>
        [HttpPost("fetch")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> FetchUpdates([FromBody] FetchRequestDto request)
        {
            // Model validation is handled by [ApiController] and [Required]
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                ModelState.AddModelError(nameof(request.Query), "Query cannot be empty or whitespace.");
                return ValidationProblem(ModelState);
            }

            try
            {
                _logger.LogInformation("API trigger for update fetch starting. Query: {Query}", request.Query);

                await _updateFetcherService.FetchAndStoreUpdatesAsync(request.Query);

                _logger.LogInformation("API trigger for update fetch completed. Query: {Query}", request.Query);

                return Ok(new ApiResponse<object> { Success = true, Message = $"Update fetch completed successfully for query: '{request.Query}'" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API trigger for update fetch failed. Query: {Query}", request.Query);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object> { Success = false, Message = "An internal server error occurred while fetching updates." });
            }
        }
    }
}