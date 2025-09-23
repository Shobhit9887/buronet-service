using Buronet.JobService.Models;
using JobService.Models;

namespace Buronet.JobService.Services.Interfaces;

public interface IJobsService
{
    Task<List<Job>> GetAsync();
    Task<List<Job>> GetJobsForJobHomeAsync();
    Task<Job?> GetAsync(string id);
    Task CreateAsync(Job newJob);
    Task<DashboardStatsDto> GetDashboardStatsAsync(string userId);
    Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync();
    Task<bool> UpdateAsync(string id, Job updatedJob);
}