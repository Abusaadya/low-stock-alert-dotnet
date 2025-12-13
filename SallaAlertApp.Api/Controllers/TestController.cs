using Microsoft.AspNetCore.Mvc;
using SallaAlertApp.Api.Services;

namespace SallaAlertApp.Api.Controllers;

[Route("test")]
public class TestController : BaseController
{
    private readonly WhatsAppService _whatsapp;

    public TestController(WhatsAppService whatsapp)
    {
        _whatsapp = whatsapp;
    }

    [HttpGet("whatsapp")]
    public async Task<IActionResult> TestWhatsApp([FromQuery] string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return BadRequest("Please provide ?phone=+201234567890");

        try
        {
            var message = "🧪 Test message from Salla Alert App!\n\nIf you received this, WhatsApp integration is working! ✅";
            var result = await _whatsapp.SendWhatsAppMessage(phone, message);
            
            return Ok(new { 
                success = result, 
                message = result ? "Message sent successfully!" : "Failed to send message. Check Twilio credentials." 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}
