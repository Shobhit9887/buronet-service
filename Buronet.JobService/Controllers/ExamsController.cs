using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
using JobService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Buronet.JobService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamsController : ControllerBase
    {
        private readonly IExamsService _examsService;

        public ExamsController(IExamsService examsService)
        {
            _examsService = examsService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Exam>>> Get()
        {
            var exams = await _examsService.GetAsync();
            if (exams == null)
            {
                return NotFound();
            }
            return Ok(exams);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (exams, totalCount) = await _examsService.GetPaginatedAsync(page, pageSize);

            return Ok(new
            {
                Page = page < 1 ? 1 : page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Data = exams
            });
        }

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Exam>> Get(string id)
        {
            var exam = await _examsService.GetAsync(id);
            if (exam == null)
            {
                return NotFound();
            }
            return Ok(exam);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Exam newExam)
        {
            if (newExam == null)
            {
                return BadRequest();
            }
            await _examsService.CreateAsync(newExam);
            return CreatedAtAction(nameof(Get), new { id = newExam.Id }, newExam);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, [FromBody] Exam updatedExam)
        {
            if (updatedExam == null)
            {
                return BadRequest();
            }

            var existingExam = await _examsService.GetAsync(id);
            if (existingExam == null)
            {
                return NotFound();
            }

            await _examsService.UpdateAsync(id, updatedExam);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiSearchResponse<List<Exam>>>> Search([FromQuery] string keyword)
        {
            // 1. Validate that a keyword was provided.
            if (string.IsNullOrWhiteSpace(keyword))
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest(new ApiSearchResponse<List<Exam>> { Success = false, Message = "A search keyword is required." });
                }

            // 2. Call the service method to perform the search.
            var jobs = await _examsService.SearchAsync(keyword);

            // 3. Return the results in a standard API response format.
            return Ok(new ApiSearchResponse<List<Exam>> { Success = true, Data = jobs });
        }
    }
}
