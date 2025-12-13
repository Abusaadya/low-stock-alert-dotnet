using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("settings")]
    public async Task<IActionResult> ViewSettings([FromServices] Data.ApplicationDbContext context)
    {
        var merchant = await context.Merchants
            .OrderByDescending(m => m.UpdatedAt)
            .FirstOrDefaultAsync();

        if (merchant == null)
            return NotFound("No merchant found");

        return Ok(new
        {
            merchantId = merchant.MerchantId,
            alertThreshold = merchant.AlertThreshold,
            alertEmail = merchant.AlertEmail,
            telegramChatId = merchant.TelegramChatId,
            notifyEmail = merchant.NotifyEmail,
            notifyWebhook = merchant.NotifyWebhook,
            customWebhookUrl = merchant.CustomWebhookUrl,
            updatedAt = merchant.UpdatedAt
        });
    }
}
