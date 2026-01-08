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
    private readonly IHttpClientFactory _httpClientFactory;

    public static string? LastPayload;
    public static DateTime? LastPayloadTime;

    public WebhooksController(ApplicationDbContext context, TelegramService telegramService, EmailService emailService, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _telegramService = telegramService;
        _emailService = emailService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("app-events")]
    public async Task<IActionResult> Index([FromBody] SallaWebhookPayload payload)
    {
        // Debugging: Capture last payload
        LastPayload = JsonSerializer.Serialize(payload);
        LastPayloadTime = DateTime.UtcNow;

        Console.WriteLine($"[Webhook] Received the event: {payload.Event}");

        if (payload.Event == "app.store.authorize")
        {
            Console.WriteLine($"[Webhook] Authorization Event for Merchant: {payload.Merchant}");

            var merchantId = payload.Merchant;
            var accessToken = payload.Data.AccessToken;
            var refreshToken = payload.Data.RefreshToken;
            var expiresIn = payload.Data.ExpiresIn ?? 1209600; // Default 14 days

            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("[Webhook] Error: No access token in payload");
                return BadRequest("No access token provided");
            }

            // Fetch Merchant Email from Salla
            string? merchantEmail = await GetMerchantEmail(accessToken);
            Console.WriteLine($"[Webhook] Fetched the Merchant Email: {merchantEmail ?? "Not Found"}");

            // 1. Save Merchant Info
            var authMerchant = await _context.Merchants.FindAsync(merchantId);
            if (authMerchant == null)
            {
                authMerchant = new Merchant
                {
                    MerchantId = merchantId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken ?? "",
                    ExpiresIn = expiresIn,
                    AlertEmail = merchantEmail ?? "", // Use fetched email
                    NotifyEmail = !string.IsNullOrEmpty(merchantEmail), // Enable email alerts if found
                    Name = $"Store-{merchantId}"
                };
                _context.Merchants.Add(authMerchant);
            }
            else
            {
                authMerchant.AccessToken = accessToken;
                authMerchant.RefreshToken = refreshToken ?? authMerchant.RefreshToken;
                authMerchant.ExpiresIn = expiresIn;
                // Only update email if we found one and it wasn't set or user wants updates? 
                // Let's only set it if empty to avoid overwriting user preference if they changed it manually later.
                if (string.IsNullOrEmpty(authMerchant.AlertEmail) && !string.IsNullOrEmpty(merchantEmail))
                {
                    authMerchant.AlertEmail = merchantEmail;
                    authMerchant.NotifyEmail = true;
                }
            }
            await _context.SaveChangesAsync();

            // 2. Create Trial Subscription (if new)
            var subService = HttpContext.RequestServices.GetService<SubscriptionService>();
            if (subService != null)
            {
                var existingSub = await _context.Subscriptions.FirstOrDefaultAsync(s => s.MerchantId == merchantId);
                if (existingSub == null)
                {
                     var subscription = new Subscription
                    {
                        MerchantId = merchantId,
                        PlanType = PlanType.Free,
                        Status = SubscriptionStatus.Trial,
                        StartDate = DateTime.UtcNow,
                        TrialEndsAt = DateTime.UtcNow.AddDays(7),
                        MaxTelegramAccounts = 1,
                        MaxAlertsPerMonth = 50,
                        AlertsSentThisMonth = 0,
                        LastResetDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Subscriptions.Add(subscription);
                    await _context.SaveChangesAsync();
                }
            }
            
            // 3. Send Welcome Email with Settings Link
            if (!string.IsNullOrEmpty(merchantEmail))
            {
                _ = Task.Run(async () => 
                {
                    try 
                    {
                        var settingsLink = $"https://{Request.Host}/settings?merchant={merchantId}";
                        var subject = "مرحباً بك في منبه المخزون! 🚀";
                        var body = $@"
                            <h2>تم تثبيت التطبيق بنجاح!</h2>
                            <p>أهلاً بك،</p>
                            <p>تطبيق منبه المخزون جاهز الآن لمساعدتك في متابعة منتجاتك.</p>
                            <p><strong>لضبط الإعدادات وربط تليجرام، اضغط على الرابط التالي:</strong></p>
                            <p><a href='{settingsLink}' style='background:#00b9ff; color:white; padding:10px 20px; text-decoration:none; border-radius:5px;'>⚙️ ضبط الإعدادات</a></p>
                            <p>أو انسخ الرابط: {settingsLink}</p>
                            <br>
                            <p>مع تحيات فريق منبه المخزون</p>
                        ";
                        await _emailService.SendEmailAsync(merchantEmail, subject, body);
                        Console.WriteLine($"[Webhook] Welcome email sent to {merchantEmail}");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"[Webhook] Error sending welcome email: {ex.Message}");
                    }
                });
            }

            return Ok(new { message = "Authorized successfully" });
        }

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

    private async Task<string?> GetMerchantEmail(string accessToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://accounts.salla.sa/oauth2/user/info");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("email", out var email))
                    {
                        return email.GetString();
                    }
                }
            }
            else 
            {
                Console.WriteLine($"[Webhook] Failed to fetch user info. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Webhook] Error fetching merchant email: {ex.Message}");
        }
        return null; // Return null if not found
    }
}
