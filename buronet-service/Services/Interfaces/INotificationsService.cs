using buronet_service.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace buronet_service.Services
{
    public interface INotificationsService
    {
        Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, int limit = 20);
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
        Task CreateNotificationAsync(Notification notification);
    }
}