using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace buronet_service.Models
{
    // Enum to categorize the source/type of the notification
    public enum NotificationType
    {
        ConnectionAccepted,
        NewJobMatchingBookmark,
        ExamDateUpdated,
        JobBookmarkAdded,
        ExamBookmarkAdded,
        PostLiked,
        ConnectionRequestReceived,
        JobExpired
    }

    public class Notification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; } // The user who receives the notification

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;

        [StringLength(500)]
        public string Message { get; set; } = null!;

        [Required]
        public NotificationType Type { get; set; }

        // Link to the specific resource (e.g., /job/GUID, /profile/GUID)
        [StringLength(200)]
        public string RedirectUrl { get; set; } = null!;

        // The ID of the related item (e.g., JobId, OtherUserId, ExamId)
        public string? TargetId { get; set; }

        [Required]
        public bool IsRead { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // DTO for sending data to the frontend
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!; // String representation of the enum
        public string RedirectUrl { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } // Calculated on the server
    }

    public class InternalNotificationCreateDto
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string RedirectUrl { get; set; } = null!;
        public string? TargetId { get; set; }
    }
}