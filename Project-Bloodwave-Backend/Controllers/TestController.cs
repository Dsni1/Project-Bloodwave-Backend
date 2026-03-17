using Microsoft.AspNetCore.Mvc;

namespace Project_Bloodwave_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // GET /api/test/ping
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            var result = new
            {
                ok = true,
                message = "Bloodwave API is alive",
                utc = DateTimeOffset.UtcNow,
                server = Environment.MachineName,
                random = Random.Shared.Next(1, 1_000_000)
            };

            return Ok(result);
            //mükszik:)
        }
    }
}