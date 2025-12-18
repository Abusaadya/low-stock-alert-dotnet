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
    private readonly EmailService _emailService;

    public static string? LastPayload { get; private set; }
    public static DateTime? LastPayloadTime { get; private set; }

    public WebhooksController(ApplicationDbContext context, TelegramService telegramService, EmailService emailService)
    {
        _context = context;
        _telegramService = telegramService;
        _emailService = emailService;
    }

    [HttpPost("app-events")]
    public async Task<IActionResult> Index([FromBody] SallaWebhookPayload payload)
    {
        // Debugging: Capture last payload
        LastPayload = JsonSerializer.Serialize(payload);
        LastPayloadTime = DateTime.UtcNow;

        Console.WriteLine($"[Webhook] Received event: {payload.Event}");

        // 1. Filter Event
        if (payload.Event == "app.subscription.started" || payload.Event == "app.subscription.renewed")
        {
            var merchantId = payload.Merchant;
            var planName = payload.Data.PlanName ?? "Basic"; // Fallback
            var endDate = payload.Data.EndDate;

            Console.WriteLine($"[Webhook] Subscription Update: {merchantId} -> {planName}");
            
            var subService = HttpContext.RequestServices.GetService<SubscriptionService>();
            if (subService != null)
            {
                PlanType plan = PlanType.Basic;
                if (planName.Contains("Pro", StringComparison.OrdinalIgnoreCase)) plan = PlanType.Pro;
                if (planName.Contains("Basic", StringComparison.OrdinalIgnoreCase)) plan = PlanType.Basic;
                
                await subService.UpgradePlan(merchantId, plan);
            }
            return Ok(new { message = "Subscription updated" });
        }
        
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

        if (quantity <= merchant.AlertThreshold)
        {
            // ENFORCE QUOTA (Shared between Telegram and Email for simplicity, or we can separate)
            var subService = HttpContext.RequestServices.GetService<SubscriptionService>();
            if (subService != null)
            {
                bool canSend = await subService.CanSendAlert(merchant.MerchantId);
                if (!canSend)
                {
                    Console.WriteLine("[Webhook] Alert quota exceeded for this month.");
                    return Ok(new { message = "Quota exceeded", merchant_id = payload.Merchant });
                }
                
                await subService.IncrementAlertCount(merchant.MerchantId);
            }

            // 4. FIRE AND FORGET: Handle Notifications in Background
            // "Double Safety" pattern: Each task has its own try-catch so one failure doesn't affect the other.
            _ = Task.Run(async () =>
            {
                // Task A: Telegram (Isolated)
                var telegramTask = Task.Run(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(merchant.TelegramChatId))
                        {
                            Console.WriteLine("[Background] Sending Telegram alert...");
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
                                await _telegramService.SendMessageAsync(chatId.Trim(), message.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Telegram Background Error] {ex.Message}");
                    }
                });

                // Task B: Email (Isolated)
                var emailTask = Task.Run(async () =>
                {
                    try
                    {
                        if (merchant.NotifyEmail && !string.IsNullOrEmpty(merchant.AlertEmail))
                        {
                            Console.WriteLine($"[Background] Sending Email alert to {merchant.AlertEmail}...");
                            var emailSubject = $"تنبيه مخزون: {payload.Data.Name}";
                            var emailBody = $@"
                                <h2>⚠️ تنبيه: مخزون منخفض</h2>
                                <p><strong>المنتج:</strong> {payload.Data.Name}</p>
                                <p><strong>الكمية الحالية:</strong> {quantity}</p>
                                <p><strong>الحد الأدنى:</strong> {merchant.AlertThreshold}</p>
                                <p><a href='{payload.Data.Urls?.Customer ?? "#"}'>عرض المنتج</a></p>
                            ";
                            
                            await _emailService.SendEmailAsync(merchant.AlertEmail, emailSubject, emailBody);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Email Background Error] {ex.Message}");
                    }
                });

                // Wait for both tasks (just to ensure the thread stays alive long enough if needed, though Fire & Forget handles it)
                await Task.WhenAll(telegramTask, emailTask);
            });
            
            return Ok(new { message = "Alerts processing in background" });
        }

        else 
        {
            Console.WriteLine("[Webhook] Quantity is sufficient. No alert needed.");
        }

        return Ok(new { message = "Quantity sufficient", current = quantity, threshold = merchant.AlertThreshold });
    }
}
