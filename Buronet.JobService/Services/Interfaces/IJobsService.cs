using Buronet.JobService.Models;
using JobService.Models;

namespace Buronet.JobService.Services.Interfaces;

public interface IJobsService
{
    Task<List<Job>> GetAsync();
    Task<List<Job>> GetJobsForJobHomeAsync();
    Task<Job?> GetAsync(string id);
    Task CreateAsync(Job newJob);
    Task<JobDashboardStatsDto> GetJobDashboardStatsAsync(string userId);
    Task<ExamDashboardStatsDto> GetExamDashboardStatsAsync(string userId);
    Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync();
    Task<bool> UpdateAsync(string id, Job updatedJob);
    Task<List<Job>> SearchAsync(string keyword);
}