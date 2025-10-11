using System.ComponentModel.DataAnnotations;

namespace Buronet.JobService.Controllers;

public class AddBookmarkRequest
{
    [Required]
    public string Id { get; set; } = null!;
}
