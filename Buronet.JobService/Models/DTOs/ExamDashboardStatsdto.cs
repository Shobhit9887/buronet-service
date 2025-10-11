// File: Buronet.JobService/Models/DashboardStatsDto.cs

namespace Buronet.JobService.Models;

public class ExamDashboardStatsDto
{
    public long TotalActiveExams { get; set; }
    public long NewExamsToday { get; set; }
    public long TotalBookmarkedExams { get; set; } // For a specific user
    // Add other stats as needed, e.g., NewNotifications
}