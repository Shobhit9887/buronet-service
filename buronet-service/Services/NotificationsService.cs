using AutoMapper; // Assuming you use AutoMapper for DTO mapping
using buronet_service.Data; // Your DbContext
using buronet_service.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace buronet_service.Services
{
    public class NotificationsService : INotificationsService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public NotificationsService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(Guid userId, int limit = 20)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();

            // Map to DTO and calculate TimeAgo
            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                RedirectUrl = n.RedirectUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                // Simple TimeAgo calculation (can be made more robust)
                TimeAgo = GetTimeAgo(n.CreatedAt)
            }).ToList();
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null || notification.IsRead) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Utility method for TimeAgo calculation
        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow.Subtract(dateTime);
            if (timeSpan.TotalMinutes < 1) return "just now";
            if (timeSpan.TotalHours < 1) return $"{timeSpan.Minutes}m ago";
            if (timeSpan.TotalDays < 1) return $"{timeSpan.Hours}h ago";
            if (timeSpan.TotalDays < 30) return $"{timeSpan.Days}d ago";
            return dateTime.ToString("MMM dd, yyyy");
        }
    }
}