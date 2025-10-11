using System.ComponentModel.DataAnnotations;

namespace Buronet.JobService.Models;

/// <summary>
/// Represents a user's bookmark for a job. This is an EF Core entity for MySQL.
/// </summary>
public class UserJobBookmark
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string JobId { get; set; } = null!;

    public DateTime SavedDate { get; set; } = DateTime.UtcNow;
}
