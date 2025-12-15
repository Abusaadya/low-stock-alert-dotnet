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

        Console.WriteLine($"[Webhook] Received event: {payload.Event}");

        // 1. Filter Event
        if (payload.Event != "product.updated")
        {
            Console.WriteLine("[Webhook] Ignored event type.");
            return Ok(new { message = "Ignored event", event_type = payload.Event });
        }

        // 2. Find Merchant
        Console.WriteLine($"[Webhook] Looking for Merchant ID: {payload.Merchant}");
        var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.MerchantId == payload.Merchant);
        
        if (merchant == null)
        {
            Console.WriteLine("[Webhook] Merchant not found in DB.");
            return Ok(new { message = "Merchant not found", merchant_id = payload.Merchant });
        }

        // 3. Check Quantity Logic
        int quantity = payload.Data.Quantity ?? 0;
        Console.WriteLine($"[Webhook] Product: {payload.Data.Name} (SKU: {payload.Data.Sku})");
        Console.WriteLine($"[Webhook] Quantity: {quantity}, Threshold: {merchant.AlertThreshold}");
        Console.WriteLine($"[Webhook] TelegramChatId: '{merchant.TelegramChatId}'");

        if (quantity <= merchant.AlertThreshold)
        {
            // 4. Send Notification (Telegram)
            if (!string.IsNullOrEmpty(merchant.TelegramChatId))
            {
                Console.WriteLine("[Webhook] Sending Telegram alert...");
                
                var productUrl = payload.Data.Urls?.Customer ?? "#";
                var message = new StringBuilder();
                message.AppendLine("⚠️ *تنبيه: مخزون منخفض*");
                message.AppendLine($"📦 المنتج: {payload.Data.Name}");
                message.AppendLine($"🔢 الكمية الحالية: *{quantity}*");
                message.AppendLine($"🔻 الحد الأدنى للتنبيه: {merchant.AlertThreshold}");
                message.AppendLine($"🔗 [عرض المنتج]({productUrl})");

                var chatIds = merchant.TelegramChatId.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var chatId in chatIds)
                {
                    var success = await _telegramService.SendMessageAsync(chatId.Trim(), message.ToString());
                    Console.WriteLine($"[Webhook] Sending to {chatId.Trim()}: {success}");
                }
                
                return Ok(new { message = "Alerts sent", count = chatIds.Length });
            }
            else
            {
                Console.WriteLine("[Webhook] No Telegram Chat ID linked for this merchant.");
                return Ok(new { message = "Low stock but no Telegram linked" });
            }
        }
        else 
        {
            Console.WriteLine("[Webhook] Quantity is sufficient. No alert needed.");
        }

        return Ok(new { message = "Quantity sufficient", current = quantity, threshold = merchant.AlertThreshold });
    }
}
