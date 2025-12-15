using SallaAlertApp.Api.Data;
using SallaAlertApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace SallaAlertApp.Api.Services;

public class ReportService
{
    private readonly ApplicationDbContext _context;
    private readonly TelegramService _telegram;

    public ReportService(ApplicationDbContext context, TelegramService telegram)
    {
        _context = context;
        _telegram = telegram;
    }

    public async Task SendWeeklyReport(long merchantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null || subscription.Status == SubscriptionStatus.Expired) return;

        var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant == null || string.IsNullOrEmpty(merchant.TelegramChatId)) return;

        var msg = new StringBuilder();
        msg.AppendLine("📊 *التقرير الأسبوعي*");
        msg.AppendLine($"🗓️ الفترة: أخر 7 أيام");
        msg.AppendLine("--------------");
        msg.AppendLine($"🔔 التنبيهات المرسلة هذا الشهر: *{subscription.AlertsSentThisMonth}*");
        msg.AppendLine($"📈 الحد المسموح: *{subscription.MaxAlertsPerMonth}*");
        
        // Calculate remaining
        int remaining = subscription.MaxAlertsPerMonth - subscription.AlertsSentThisMonth;
        if(subscription.MaxAlertsPerMonth > 100000) msg.AppendLine("✅ الرصيد المتبقي: *غير محدود*");
        else msg.AppendLine($"✅ الرصيد المتبقي: *{Math.Max(0, remaining)}*");

        msg.AppendLine("");
        msg.AppendLine("💡 *نصيحة:* تأكد من تحديث مخزونك باستمرار لضمان دقة التنبيهات.");

        var chatIds = merchant.TelegramChatId.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var chatId in chatIds)
        {
            await _telegram.SendMessageAsync(chatId.Trim(), msg.ToString());
        }

        subscription.LastWeeklyReportSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task SendMonthlyReport(long merchantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null) return;

        var merchant = await _context.Merchants.FirstOrDefaultAsync(m => m.MerchantId == merchantId);
        if (merchant == null || string.IsNullOrEmpty(merchant.TelegramChatId)) return;

        var msg = new StringBuilder();
        msg.AppendLine("📅 *التقرير الشهري*");
        msg.AppendLine($"🗓️ الشهر: {DateTime.UtcNow.ToString("MMMM yyyy")}");
        msg.AppendLine("--------------");
        msg.AppendLine($"🔔 إجمالي التنبيهات المرسلة: *{subscription.AlertsSentThisMonth}*");
        
        msg.AppendLine("");
        msg.AppendLine("🚀 نتمنى لك شهراً مليئاً بالمبيعات!");

        var chatIds = merchant.TelegramChatId.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var chatId in chatIds)
        {
            await _telegram.SendMessageAsync(chatId.Trim(), msg.ToString());
        }

        subscription.LastMonthlyReportSentAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
