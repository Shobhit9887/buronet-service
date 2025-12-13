// File: JobService/Controllers/JobsController.cs
using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
using JobService.Models;
using JobService.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobsService _jobsService;

    public JobsController(IJobsService jobsService) =>
        _jobsService = jobsService;

    [HttpGet]
    public async Task<ActionResult<List<Job>>> Get()
    {
        var jobs = await _jobsService.GetAsync();
        if (jobs is null)
        {
            return NotFound();
        }

        return jobs;
    }

    [HttpGet("job-home")]
    public async Task<ActionResult<List<Job>>> GetJobsForJobHome()
    {
        var jobs = await _jobsService.GetAsync();
        if (jobs is null)
        {
            return NotFound();
        }

        return jobs;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Job>> Get(string id)
    {
        var job = await _jobsService.GetAsync(id);

        if (job is null)
        {
            return NotFound();
        }

        return job;
    }

    [HttpPost]
    public async Task<IActionResult> Post(Job newJob)
    {
        await _jobsService.CreateAsync(newJob);
        // Returns a 201 Created response with a link to the new resource
        return CreatedAtAction(nameof(Get), new { id = newJob.Id }, newJob);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> UpdateJob(string id, [FromBody] Job updatedJob)
    {
        // A simple check to ensure the request body is not null
        if (updatedJob == null)
        {
            return BadRequest();
        }

        var job = await _jobsService.GetAsync(id);

        if (job is null)
        {
            return NotFound();
        }

        await _jobsService.UpdateAsync(id, updatedJob);

        // 204 No Content is a standard successful response for a PUT request
        return NoContent();
    }


    [HttpGet("search")]
    public async Task<ActionResult<ApiSearchResponse<List<Job>>>> Search([FromQuery] string keyword)
    {
        // 1. Validate that a keyword was provided.
        if (string.IsNullOrWhiteSpace(keyword))
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new ApiSearchResponse<List<Job>> { Success = false, Message = "A search keyword is required." });
        }

        // 2. Call the service method to perform the search.
        var jobs = await _jobsService.SearchAsync(keyword);

        // 3. Return the results in a standard API response format.
        return Ok(new ApiSearchResponse<List<Job>> { Success = true, Data = jobs });
    }

}