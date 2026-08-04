using JobMailerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobMailerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BouncesController : ControllerBase
    {
        private readonly BounceProcessingService _bounceProcessingService;
        private readonly ILogger<BouncesController> _logger;

        public BouncesController(
            BounceProcessingService bounceProcessingService,
            ILogger<BouncesController> logger)
        {
            _bounceProcessingService = bounceProcessingService;
            _logger = logger;
        }

        [HttpPost("process-unread")]
        public async Task<IActionResult> ProcessUnread(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _bounceProcessingService.ProcessUnreadBouncesAsync(cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bounce processing failed.");
                return StatusCode(500, new { error = "Bounce processing failed. Check the server logs for details." });
            }
        }
    }
}
