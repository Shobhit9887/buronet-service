using buronet_service.Models.DTOs.Search;
using System.Threading.Tasks;

namespace buronet_service.Services.Interfaces
{
    public interface ISearchService
    {
        // The main method for unified search
        Task<SearchResultDto> UnifiedSearchAsync(string query, Guid currentUserId);
    }
}