using System.ComponentModel.DataAnnotations;

namespace buronet_service.Models.DTOs.User
{
    public class UpdateConnectionRequestStatusDto
    {
        [Required]
        [EnumDataType(typeof(ConnectionRequestStatus), ErrorMessage = "Invalid status. Must be 'Accepted' or 'Rejected'.")]
        public string Status { get; set; } = string.Empty;
    }

    // Enum to constrain allowed status values
    public enum ConnectionRequestStatus
    {
        Pending, // Not used for updates
        Accepted,
        Rejected,
        Cancelled // Not used for updates, usually a separate action
    }
}