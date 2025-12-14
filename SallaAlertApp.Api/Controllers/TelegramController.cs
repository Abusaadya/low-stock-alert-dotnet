using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Data;
using SallaAlertApp.Api.Services;
using System.Text.Json;

namespace SallaAlertApp.Api.Controllers;

[Route("telegram")]
public class TelegramController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly TelegramService _telegram;
    private readonly ILogger<TelegramController> _logger;

    public TelegramController(ApplicationDbContext context, TelegramService telegram, ILogger<TelegramController> logger)
    {
        _context = context;
        _telegram = telegram;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] JsonElement update)
    {
        try
        {
            // Simple logic to handle /start <MerchantId>
            if (update.TryGetProperty("message", out var message))
            {
                var chatId = message.GetProperty("chat").GetProperty("id").GetInt64().ToString();
                var text = message.TryGetProperty("text", out var t) ? t.GetString() : string.Empty;

                if (!string.IsNullOrEmpty(text) && text.StartsWith("/start"))
                {
                    var parts = text.Split(' ');
                    if (parts.Length > 1 && long.TryParse(parts[1], out var merchantId))
                    {
                        var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
                        if (merchant != null)
                        {
                            var existingIds = string.IsNullOrEmpty(merchant.TelegramChatId) 
                                ? new List<string>() 
                                : merchant.TelegramChatId.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                            if (!existingIds.Contains(chatId))
                            {
                                existingIds.Add(chatId);
                                merchant.TelegramChatId = string.Join(",", existingIds);
                                await _context.SaveChangesAsync();
                                await _telegram.SendMessageAsync(chatId, $"✅ تم ربط حسابك بنجاح! ستتلقى التنبيهات هنا.\n(الحسابات المتصلة: {existingIds.Count})");
                            }
                            else
                            {
                                await _telegram.SendMessageAsync(chatId, "ℹ️ هذا الحساب مرتبط بالفعل.");
                            }
                        }
                        else
                        {
                            await _telegram.SendMessageAsync(chatId, "❌ لم يتم العثور على المتجر بهذا الرقم. تأكد من الرابط.");
                        }
                    }
                    else
                    {
                        await _telegram.SendMessageAsync(chatId, "👋 أهلاً بك! يرجى استخدام رابط التفعيل من صفحة الإعدادات.");
                    }
                }
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram webhook");
            return Ok(); // Always return OK to Telegram so it stops retrying
        }
    }
}
