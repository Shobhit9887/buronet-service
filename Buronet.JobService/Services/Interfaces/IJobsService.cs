using Buronet.JobService.Models;
using Buronet.JobService.Models.DTOs;
using JobService.Models;

namespace Buronet.JobService.Services.Interfaces;

public interface IJobsService
{
    Task<List<Job>> GetAsync();
    Task<List<Job>> GetJobsForJobHomeAsync();
    Task<Job?> GetAsync(string id);
    Task CreateAsync(Job newJob);

    Task<Job> CreateFromFrontendAsync(CreateJobRequest request);

    Task<JobDashboardStatsDto> GetJobDashboardStatsAsync(string userId);
    Task<ExamDashboardStatsDto> GetExamDashboardStatsAsync(string userId);
    Task<List<DepartmentStatsDto>> GetDepartmentStatsAsync();
    Task<bool> UpdateAsync(string id, Job updatedJob);
    Task<List<Job>> SearchAsync(string keyword);
    Task<(List<Job> Jobs, long TotalCount)> GetPaginatedAsync(int page, int pageSize);
}