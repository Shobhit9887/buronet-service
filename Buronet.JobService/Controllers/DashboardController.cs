// File: Buronet.JobService/Controllers/DashboardController.cs

using Buronet.JobService.Models;
using Buronet.JobService.Services;
using Buronet.JobService.Services.Interfaces;
using JobService.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Buronet.JobService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IJobsService _jobsService;

    public DashboardController(IJobsService jobsService)
    {
        _jobsService = jobsService;
    }

    [HttpGet("job/stats/{userId}")]
    public async Task<IActionResult> GetJobStats(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID is required.");
        }

        var stats = await _jobsService.GetJobDashboardStatsAsync(userId);
        return Ok(stats);
    }

    [HttpGet("exam/stats/{userId}")]
    public async Task<IActionResult> GetExamStats(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("User ID is required.");
        }

        var stats = await _jobsService.GetExamDashboardStatsAsync(userId);
        return Ok(stats);
    }

    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartmentStats()
    {
        var stats = await _jobsService.GetDepartmentStatsAsync();
        return base.Ok(new global::JobService.Controllers.ApiResponse<List<DepartmentStatsDto>> { Success = true, Data = stats });
    }

}