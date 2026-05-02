using Microsoft.AspNetCore.Mvc;
using SchoolScheduler.Services;

namespace SchoolScheduler.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly ScheduleService _service;

        public ScheduleController(ScheduleService service)
        {
            _service = service;
        }

        [HttpGet("generate")]
        public async Task<IActionResult> Generate()
        {
            var result = await _service.GenerateWeeklyScheduleAsync();
            return Ok(result);
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Controller working");
        }
    }
}