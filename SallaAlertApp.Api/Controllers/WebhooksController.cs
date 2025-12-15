using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SallaAlertApp.Api.Data;
using SallaAlertApp.Api.Models;
using SallaAlertApp.Api.Services;
using System.Text;
using System.Text.Json;

namespace SallaAlertApp.Api.Controllers;

[ApiController]
[Route("webhooks")]
public class WebhooksController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly TelegramService _telegramService;

    public static string? LastPayload { get; private set; }
    public static DateTime? LastPayloadTime { get; private set; }

    public WebhooksController(ApplicationDbContext context, TelegramService telegramService)
    {
        _context = context;
        _telegramService = telegramService;
    }

    [HttpPost("app-events")]
    public async Task<IActionResult> Index([FromBody] SallaWebhookPayload payload)
    {
        // Debugging: Capture last payload
        LastPayload = JsonSerializer.Serialize(payload);
        LastPayloadTime = DateTime.UtcNow;

        // 1. Filter Event
        if (payload.Event != "product.updated")
        {
            return Ok(new { message = "Ignored event", event_type = payload.Event });
        }

        // 2. Find Merchant
        var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.MerchantId == payload.Merchant);
        if (merchant == null)
        {
            return Ok(new { message = "Merchant not found", merchant_id = payload.Merchant });
        }

        // 3. Check Quantity Logic
        // Salla sometimes sends quantity as null or inside options (basic check for now)
        int quantity = payload.Data.Quantity ?? 0;

        if (quantity <= merchant.AlertThreshold)
        {
            // 4. Send Notification (Telegram)
            if (!string.IsNullOrEmpty(merchant.TelegramChatId))
            {
                var productUrl = payload.Data.Urls?.Customer ?? "#";
                var message = new StringBuilder();
                message.AppendLine("⚠️ *تنبيه: مخزون منخفض*");
                message.AppendLine($"📦 المنتج: {payload.Data.Name}");
                message.AppendLine($"🔢 الكمية الحالية: *{quantity}*");
                message.AppendLine($"🔻 الحد الأدنى للتنبيه: {merchant.AlertThreshold}");
                message.AppendLine($"🔗 [عرض المنتج]({productUrl})");

                await _telegramService.SendMessageAsync(merchant.TelegramChatId, message.ToString());
                
                return Ok(new { message = "Alert sent", channel = "telegram" });
            }
            else
            {
                return Ok(new { message = "Low stock but no Telegram linked" });
            }
        }

        return Ok(new { message = "Quantity sufficient", current = quantity, threshold = merchant.AlertThreshold });
    }
}
