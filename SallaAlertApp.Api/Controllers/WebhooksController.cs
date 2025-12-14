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
            else if (eventName == "app.uninstalled")
            {
                await HandleAppUninstall(merchantId);
            }
            else if (eventName == "subscription.created" || eventName == "subscription.renewed")
            {
                await HandleSubscriptionActivated(merchantId, data);
            }
            else if (eventName == "subscription.cancelled")
            {
                await HandleSubscriptionCancelled(merchantId);
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

        // Check subscription status
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.MerchantId == merchantId);
        if (subscription == null || subscription.Status == SubscriptionStatus.Expired || subscription.Status == SubscriptionStatus.Cancelled)
        {
            _logger.LogWarning("[Alert] Merchant {Id} has no active subscription. Skipping alert.", merchantId);
            return;
        }

        // Check if trial expired
        if (subscription.Status == SubscriptionStatus.Trial && subscription.TrialEndsAt < DateTime.UtcNow)
        {
            subscription.Status = SubscriptionStatus.Expired;
            await _context.SaveChangesAsync();
            _logger.LogWarning("[Alert] Merchant {Id} trial expired. Skipping alert.", merchantId);
            return;
        }

        // Check alert quota
        if (subscription.PlanType == PlanType.Free)
        {
            if (subscription.LastResetDate.Month != DateTime.UtcNow.Month)
            {
                subscription.AlertsSentThisMonth = 0;
                subscription.LastResetDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            if (subscription.AlertsSentThisMonth >= subscription.MaxAlertsPerMonth)
            {
                _logger.LogWarning("[Alert] Merchant {Id} exceeded quota. Skipping alert.", merchantId);
                return;
            }
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
                var chatIds = merchant.TelegramChatId.Split(',', StringSplitOptions.RemoveEmptyEntries);
                _logger.LogInformation("[Telegram] Sending to {Count} accounts", chatIds.Length);

                foreach (var chatId in chatIds)
                {
                    var message = $"⚠️ *تنبيه مخزون منخفض*\n\n" +
                                 $"المنتج: *{name}*\n" +
                                 $"الكمية الحالية: {quantity}\n" +
                                 $"الحد الأدنى: {merchant.AlertThreshold}\n\n" +
                                 $"يرجى إعادة التخزين قريباً!";
                    
                    await _telegram.SendMessageAsync(chatId.Trim(), message);
                }
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

    private async Task HandleAppUninstall(long merchantId)
    {
        var merchant = await _context.Merchants.FindAsync(merchantId);
        if (merchant == null)
        {
            _logger.LogWarning("[Uninstall] Merchant {Id} not found.", merchantId);
            return;
        }

        // Option: Delete completely (Best for privacy)
        _context.Merchants.Remove(merchant);
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("[Uninstall] Merchant {Id} data deleted.", merchantId);
    }

    private async Task HandleSubscriptionActivated(long merchantId, JsonElement data)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null) return;

        var planType = data.TryGetProperty("plan", out var plan) ? plan.GetString() : null;
        
        subscription.Status = SubscriptionStatus.Active;
        subscription.StartDate = DateTime.UtcNow;
        
        if (planType?.Contains("year", StringComparison.OrdinalIgnoreCase) == true)
        {
            subscription.PlanType = PlanType.Pro;
            subscription.EndDate = DateTime.UtcNow.AddYears(1);
            subscription.MaxTelegramAccounts = int.MaxValue;
            subscription.MaxAlertsPerMonth = int.MaxValue;
        }
        else
        {
            subscription.PlanType = PlanType.Basic;
            subscription.EndDate = DateTime.UtcNow.AddMonths(1);
            subscription.MaxTelegramAccounts = 1;
            subscription.MaxAlertsPerMonth = int.MaxValue;
        }

        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("[Subscription] Activated for Merchant {MerchantId} - Plan: {Plan}", merchantId, subscription.PlanType);
    }

    private async Task HandleSubscriptionCancelled(long merchantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null) return;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("[Subscription] Cancelled for Merchant {MerchantId}", merchantId);
    }
}
