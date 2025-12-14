using Microsoft.AspNetCore.Mvc;

namespace SallaAlertApp.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController : BaseController
{
    public static string? LastPayload { get; private set; }
    public static DateTime? LastPayloadTime { get; private set; }

    [HttpPost("app-events")]
    public async Task<IActionResult> Index()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        
        LastPayload = body;
        LastPayloadTime = DateTime.UtcNow;

        return Ok("Webhook received");
    }
}
