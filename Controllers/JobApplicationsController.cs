using JobMailerApi.Models;
using JobMailerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobMailerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobApplicationsController : ControllerBase
    {
        private readonly JobMailerService _service;

        public JobApplicationsController(JobMailerService service)
        {
            _service = service;
        }

        [HttpPost]
        public IActionResult Post(JobApplicationRequest request)
        {
            try
            {
                _service.ProcessApplication(request);
                return Ok(new { message = "Application sent and logged." });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}