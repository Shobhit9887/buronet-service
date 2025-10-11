using System.ComponentModel.DataAnnotations;

namespace Buronet.JobService.Models;

public class UserExamBookmark
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string ExamId { get; set; } = null!;

    public DateTime SavedDate { get; set; } = DateTime.UtcNow;
}
