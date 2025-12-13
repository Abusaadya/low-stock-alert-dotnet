using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Data;
using System.Text.Json;

namespace SallaAlertApp.Api.Controllers;

[Route("webhooks")]
public class WebhooksController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public WebhooksController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
        _http = new HttpClient();
    }

    [HttpPost("app-events")]
    public async Task<IActionResult> HandleWebhook([FromBody] JsonElement payload)
    {
        try
        {
            // Extract core fields
            var eventName = payload.GetProperty("event").GetString();
            var merchantId = payload.GetProperty("merchant").GetInt64();
            var data = payload.GetProperty("data");

            Console.WriteLine($"[Webhook] Event: {eventName} | Merchant: {merchantId}");

            if (eventName == "product.updated")
            {
                // Logic: Check Threshold
                await ProcessProductUpdate(merchantId, data);
            }

            return Ok("Webhook Received");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Webhook Error: {ex.Message}");
            return StatusCode(500, ex.Message);
        }
    }

    private async Task ProcessProductUpdate(long merchantId, JsonElement data)
    {
        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null) return;

        var quantity = data.GetProperty("quantity").GetInt32();
        var name = data.TryGetProperty("name", out var n) ? n.GetString() : "Unknown Product";
        
        // Check Threshold
        if (quantity <= merchant.AlertThreshold)
        {
            Console.WriteLine($"[Alert] Low stock for {name}: {quantity} <= {merchant.AlertThreshold}");

            // 1. Email
            if (merchant.NotifyEmail && !string.IsNullOrEmpty(merchant.AlertEmail))
            {
                // TODO: Implement proper SMTP Service
                Console.WriteLine($"[Mock Email] Sending email to {merchant.AlertEmail}");
            }

            // 2. Automation (Make.com / Telegram)
            if (merchant.NotifyWebhook && !string.IsNullOrEmpty(merchant.CustomWebhookUrl))
            {
                await SendAutomationWebhook(merchant, name, quantity, data);
            }
        }
    }

    private async Task SendAutomationWebhook(Models.Merchant merchant, string productName, int qty, JsonElement rawData)
    {
        var payload = new
        {
            event_name = "low_stock_alert",
            product_name = productName,
            current_quantity = qty,
            threshold = merchant.AlertThreshold,
            merchant_telegram_chat_id = merchant.TelegramChatId,
            timestamp = DateTime.UtcNow,
            raw_salla_data = rawData
        };

        try
        {
            await _http.PostAsJsonAsync(merchant.CustomWebhookUrl, payload);
            Console.WriteLine("[Automation] Webhook sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Automation] Failed to send webhook: {ex.Message}");
        }
    }
}
