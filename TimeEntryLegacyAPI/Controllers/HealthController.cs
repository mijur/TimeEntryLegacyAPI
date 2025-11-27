using Microsoft.AspNetCore.Mvc;

namespace TimeEntryLegacyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.Now });
        }
    }
}
