using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Services;

namespace SallaAlertApp.Api.Controllers;

[Route("test")]
public class TestController : BaseController
{
    private readonly TelegramService _telegram;

    public TestController(TelegramService telegram)
    {
        _telegram = telegram;
    }

    [HttpGet("telegram")]
    public async Task<IActionResult> TestTelegram([FromQuery] string chatId)
    {
        if (string.IsNullOrEmpty(chatId))
            return BadRequest("Please provide ?chatId=12345");

        try
        {
            var message = "🧪 Test message from Salla Alert App!\n\nIf you received this, Telegram integration is working! ✅";
            var result = await _telegram.SendMessageAsync(chatId, message);
            
            return Ok(new { 
                success = result, 
                message = result ? "Message sent successfully!" : "Failed to send message. Check Bot Token." 
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

    [HttpGet("last-webhook")]
    public IActionResult GetLastWebhook()
    {
        if (Controllers.WebhooksController.LastPayload == null)
            return NotFound("No webhook received yet");

        return Ok(new
        {
            receivedAt = Controllers.WebhooksController.LastPayloadTime,
            payload = Controllers.WebhooksController.LastPayload
        });
    }
}
