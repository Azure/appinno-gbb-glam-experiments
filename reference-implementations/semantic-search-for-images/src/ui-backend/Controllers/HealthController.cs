using Microsoft.AspNetCore.Mvc;
using ui_backend.Services;

namespace ui_backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IDatabaseService databaseService, ILogger<HealthController> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        [HttpGet("startup")]
        public IActionResult StartupProbe()
        {
            _logger.LogTrace("Checking startup probe.");
            return Ok();
        }

        [HttpGet("readiness")]
        public async Task<IActionResult> ReadinessProbe()
        {
            _logger.LogTrace("Checking readiness probe.");
            bool isReady;
            try
            {
                isReady = await _databaseService.IsReady();
            }
            catch (Exception e)
            {
                _logger.LogDebug("Exception thrown when checking database service readiness: [{message}] {stacktrace}", e.Message, e.StackTrace);
                return BadRequest("Service not ready.");
            }
            if (isReady)
                return Ok();
            else
            {
                _logger.LogDebug("No exceptions thrown, but database service reported it was not ready.");
                return BadRequest("Service not ready.");
            }
        }

        [HttpGet("liveness")]
        public IActionResult LivenessProbe()
        {
            _logger.LogTrace("Checking liveness probe.");
            return Ok();
        }
    }
}