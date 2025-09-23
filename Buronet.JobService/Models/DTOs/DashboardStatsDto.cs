// File: Buronet.JobService/Models/DashboardStatsDto.cs

namespace Buronet.JobService.Models;

public class DashboardStatsDto
{
    public long TotalActiveJobs { get; set; }
    public long NewJobsToday { get; set; }
    public long TotalBookmarkedJobs { get; set; } // For a specific user
    // Add other stats as needed, e.g., NewNotifications
}