using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SallaAlertApp.Api.Models;

public class Merchant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("merchant_id")]
    public long MerchantId { get; set; }

    [Required]
    [Column("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    [Column("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [Column("expires_in")]
    public int ExpiresIn { get; set; }

    [Column("alert_threshold")]
    public int AlertThreshold { get; set; } = 5;

    [Column("alert_email")]
    public string? AlertEmail { get; set; }

    [Column("notify_email")]
    public bool NotifyEmail { get; set; } = true;

    [Column("custom_webhook_url")]
    public string? CustomWebhookUrl { get; set; }
    
    [Column("telegram_chat_id")]
    public string? TelegramChatId { get; set; }

    [Column("notify_webhook")]
    public bool NotifyWebhook { get; set; } = false;

    [Column("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
