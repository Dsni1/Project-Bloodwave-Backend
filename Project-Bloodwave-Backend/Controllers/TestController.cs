using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Services;

namespace Project_Bloodwave_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IMailService _mailService;

        public TestController(IMailService mailService)
        {
            _mailService = mailService;
        }

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
        }

        // POST /api/test/send-mail
        [HttpPost("send-mail")]
        public async Task<IActionResult> SendMail([FromBody] SendMailDto dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mailService.SendEmailAsync(
                dto.To,
                dto.Subject,
                dto.Text,
                dto.Html,
                cancellationToken);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.Message, detail = result.ProviderResponse });

            return Ok(new { success = true, message = result.Message });
        }
    }
}