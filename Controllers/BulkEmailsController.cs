using Microsoft.AspNetCore.Mvc;
using JobMailerApi.Services;

namespace JobMailerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BulkEmailsController : ControllerBase
    {
        private readonly BulkEmailService _bulkEmailService;
        private readonly ILogger<BulkEmailsController> _logger;

        public BulkEmailsController(
            BulkEmailService bulkEmailService,
            ILogger<BulkEmailsController> logger)
        {
            _bulkEmailService = bulkEmailService;
            _logger = logger;
        }
        [HttpPost("process-pending")]
        public async Task<IActionResult> ProcessPending(
            [FromQuery] int takeCount = 1,
            CancellationToken cancellationToken = default)
        {
            if (takeCount is < 1 or > 50)
            {
                return BadRequest(new { error = "takeCount must be between 1 and 50." });
            }

            try
            {
                var result = await _bulkEmailService.ProcessPendingEmailsAsync(takeCount, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk email processing failed before the batch completed.");
                return StatusCode(500, new { error = "Bulk email processing failed. Check the server logs for details." });
            }
        }

    }
}
