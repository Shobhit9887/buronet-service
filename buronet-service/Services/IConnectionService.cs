using buronet_service.Models.DTOs.User; // DTOs
using buronet_service.Models.User; // Entities (for clarity, though not used directly here)
using System; // For Guid
using System.Collections.Generic;
using System.Threading.Tasks;

namespace buronet_service.Services // Ensure this namespace is correct
{
    public interface IConnectionService
    {
        // Network Metrics
        Task<NetworkMetricsDto> GetNetworkMetricsAsync(Guid userId);

        // User Discovery & Connections
        Task<IEnumerable<UserCardDto>> GetDiscoverUsersAsync(Guid currentUserId);
        Task<IEnumerable<ConnectionDto>> GetUserConnectionsAsync(Guid userId);
        Task<IEnumerable<ConnectionRequestDto>> GetPendingConnectionRequestsAsync(Guid userId, bool outgoing = false);

        // Connection Management
        Task<ConnectionRequestDto?> SendConnectionRequestAsync(Guid senderId, SendConnectionRequestDto sendDto);
        Task<bool> UpdateConnectionRequestStatusAsync(Guid receiverId, int requestId, UpdateConnectionRequestStatusDto updateDto);
        Task<bool> RemoveConnectionAsync(Guid currentUserId, Guid connectedUserId); // Remove an established connection
        Task<Dictionary<string, List<SuggestedUserDto>>> GetSuggestedUsersAsync(Guid currentUserId, int limit);
        Task<IEnumerable<SuggestedUserDto>> GetGeneralSuggestedUsersAsync(Guid currentUserId, int limit);
        Task<IEnumerable<PopularUserDto>> GetPopularUsersAsync(Guid currentUserId, int limit);
    }
}