using buronet_messaging_service.Models;
using buronet_messaging_service.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace buronet_messaging_service.Services.Interfaces
{
    public interface IMessageService
    {
        Task<MessageDto> AddMessageAsync(int conversationId, Guid senderId, string content, string? clientId = null);
        Task<IEnumerable<MessageDto>> GetConversationMessagesAsync(int conversationId, Guid userId);
        // Add methods for deleting messages, marking as read, etc.
    }
}