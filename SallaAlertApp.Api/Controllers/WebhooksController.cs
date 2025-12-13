using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Data;
using System.Text.Json;

namespace SallaAlertApp.Api.Controllers;

[Route("webhooks")]
public class WebhooksController : BaseController
{
    // Static fields for debugging (store last webhook)
    public static string? LastPayload { get; set; }
    public static DateTime? LastPayloadTime { get; set; }

    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly Services.TelegramService _telegram;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(ApplicationDbContext context, IConfiguration config, Services.TelegramService telegram, ILogger<WebhooksController> logger)
    {
        _context = context;
        _config = config;
        _http = new HttpClient();
        _telegram = telegram;
        _logger = logger;
    }

    [HttpPost("app-events")]
    public async Task<IActionResult> HandleWebhook([FromBody] JsonElement payload)
    {
        try
        {
            // Store payload for debugging
            LastPayload = payload.ToString();
            LastPayloadTime = DateTime.UtcNow;
            // Extract core fields
            var eventName = payload.GetProperty("event").GetString();
            var merchantId = payload.GetProperty("merchant").GetInt64();
            var data = payload.GetProperty("data");

            _logger.LogInformation("[Webhook] Event: {EventName} | Merchant: {MerchantId}", eventName, merchantId);

            if (eventName == "product.updated")
            {
                // Logic: Check Threshold
                await ProcessProductUpdate(merchantId, data);
            }

            return Ok("Webhook Received");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook Error: {Message}", ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    private async Task ProcessProductUpdate(long merchantId, JsonElement data)
    {
        // Safely try to get quantity
        if (!data.TryGetProperty("quantity", out var qtyElement))
        {
            _logger.LogWarning("[Warning] No 'quantity' field in product data. Skipping alert.");
            return;
        }

        var quantity = qtyElement.GetInt32();
        var name = data.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown Product" : "Unknown Product";

        _logger.LogInformation(">>> [Debug] Incoming Webhook Data -> MerchantId: {MerchantId} | Product: {Name} | Quantity: {Quantity}", merchantId, name, quantity);

        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null) 
        {
            _logger.LogWarning("[Error] Merchant {Id} not found in database. Cannot process webhook.", merchantId);
            return;
        }

        // Log the full data payload to see what Salla sends
        _logger.LogInformation("[Debug] Full product data: {Data}", data.ToString());
        
        // Check Threshold
        if (quantity <= merchant.AlertThreshold)
        {
            _logger.LogInformation("[Alert] Low stock for {Name}: {Quantity} <= {Threshold}", name, quantity, merchant.AlertThreshold);

            // 1. Email
            if (merchant.NotifyEmail && !string.IsNullOrEmpty(merchant.AlertEmail))
            {
                // TODO: Implement proper SMTP Service
                _logger.LogInformation("[Mock Email] Sending email to {Email}", merchant.AlertEmail);
            }

            // 2. Telegram
            if (!string.IsNullOrEmpty(merchant.TelegramChatId)) 
            {
                _logger.LogInformation("[Telegram] Attempting to send to: {ChatId}", merchant.TelegramChatId);
                var message = $"⚠️ *تنبيه مخزون منخفض*\n\n" +
                             $"المنتج: *{name}*\n" +
                             $"الكمية الحالية: {quantity}\n" +
                             $"الحد الأدنى: {merchant.AlertThreshold}\n\n" +
                             $"يرجى إعادة التخزين قريباً!";
                
                await _telegram.SendMessageAsync(merchant.TelegramChatId, message);
            }

            // 3. Automation (Make.com / Telegram)
            if (merchant.NotifyWebhook && !string.IsNullOrEmpty(merchant.CustomWebhookUrl))
            {
                await SendAutomationWebhook(merchant, name, quantity, data);
            }
        }
        else
        {
            _logger.LogInformation("[Info] Quantity {Quantity} is above threshold {Threshold}. No alert sent.", quantity, merchant.AlertThreshold);
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
