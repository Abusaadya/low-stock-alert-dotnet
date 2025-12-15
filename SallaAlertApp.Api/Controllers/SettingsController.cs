using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Data;
using SallaAlertApp.Api.Models;

namespace SallaAlertApp.Api.Controllers;

[Route("settings")]
public class SettingsController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly Services.TelegramService _telegram;

    public SettingsController(ApplicationDbContext context, Services.TelegramService telegram)
    {
        _context = context;
        _telegram = telegram;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSettings([FromQuery] long? merchantId)
    {
        if (merchantId == null || merchantId == 0)
        {
            return BadRequest("Merchant ID is required.");
        }

        var merchant = await _context.Merchants.FindAsync(merchantId);

        if (merchant == null) return NotFound("Merchant not found in database.");

        var botUsername = await _telegram.GetBotUsernameAsync();

        return Ok(new
        {
            merchant.MerchantId,
            merchant.Name,
            merchant.AlertThreshold,
            merchant.AlertEmail,
            merchant.NotifyEmail,
            merchant.CustomWebhookUrl,
            merchant.TelegramChatId, // To check if connected
            merchant.TelegramChatId, // To check if connected
            merchant.NotifyWebhook,
            BotUsername = botUsername ?? "SallaStockAlertBot", // Fallback if fetch fails
            Subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.MerchantId == merchant.MerchantId)
        });
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings([FromBody] SettingsRequest request)
    {
        Merchant? merchant;
        
        if (request.MerchantId == 0)
        {
            // Fallback: Use the most recently updated merchant
            merchant = await _context.Merchants
                .OrderByDescending(m => m.UpdatedAt)
                .FirstOrDefaultAsync();
            
            if (merchant == null) return BadRequest("No merchants found. Please install the app first.");
        }
        else
        {
            merchant = await _context.Merchants.FindAsync(request.MerchantId);
            if (merchant == null) return NotFound("Merchant not found");
        }

        // Update fields
        merchant.AlertThreshold = request.AlertThreshold;
        merchant.AlertEmail = request.AlertEmail;
        merchant.NotifyEmail = request.NotifyEmail;
        merchant.CustomWebhookUrl = request.CustomWebhookUrl;
        merchant.TelegramChatId = request.TelegramChatId;
        merchant.NotifyWebhook = request.NotifyWebhook;
        
        merchant.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok("Settings Updated");
    }

    [HttpPost("telegram/disconnect")]
    public async Task<IActionResult> DisconnectTelegram([FromBody] SettingsRequest request)
    {
        var merchant = await _context.Merchants.FindAsync(request.MerchantId);
        if (merchant == null) return NotFound("Merchant not found");

        merchant.TelegramChatId = null;
        await _context.SaveChangesAsync();

        return Ok("Disconnected");
    }
}

public class SettingsRequest
{
    public long MerchantId { get; set; }
    public int AlertThreshold { get; set; }
    public string? AlertEmail { get; set; }
    public bool NotifyEmail { get; set; }
    public string? CustomWebhookUrl { get; set; }
    public string? TelegramChatId { get; set; }
    public bool NotifyWebhook { get; set; }
}
