using Buronet.JobService.Models;
using Buronet.JobService.Services.Interfaces;
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
    }
}
