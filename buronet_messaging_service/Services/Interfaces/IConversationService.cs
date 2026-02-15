using buronet_messaging_service.Models;
using buronet_messaging_service.Models.DTOs; // Assuming DTOs for messaging
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace buronet_messaging_service.Services.Interfaces
{
    public interface IConversationService
    {
        Task<ConversationDto> CreateConversationAsync(List<Guid> participantUserIds, string? title = null);
        Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(Guid userId);
        Task<IEnumerable<int>> GetUserConversationIdsAsync(Guid userId); // For SignalR group management
        Task<ConversationDto?> GetConversationByIdAsync(int conversationId, Guid userId);
        Task MarkConversationAsReadAsync(int conversationId, Guid userId);
        // Add methods for updating conversation, removing participants, etc.
    }
}