using System.Security.Claims; // For accessing user claims
using buronet_service.Models.DTOs.Search;
using buronet_service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using buronet_service.Services;

namespace buronet_service.Controllers
{
    [Authorize] // Ensure only authenticated users can use the search
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly AuthService _authService;

        public SearchController(ISearchService searchService, AuthService authService)
        {
            _searchService = searchService;
            _authService = authService;
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

        // GET /api/search?q=searchterm
        [HttpGet]
        public async Task<ActionResult<SearchResultDto>> GetSearchResults([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                // Return an empty result set for an empty query
                return Ok(new SearchResultDto());
            }

            // You must have a utility extension to safely get the user's ID
            Guid currentUserId = (Guid)GetCurrentUserId();

            var results = await _searchService.UnifiedSearchAsync(q, currentUserId);

            return Ok(results);
        }
    }
}