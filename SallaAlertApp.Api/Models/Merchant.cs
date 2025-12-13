using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SallaAlertApp.Api.Models;

public class Merchant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // Salla ID is provided, not auto-generated
    public long MerchantId { get; set; }

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    public int ExpiresIn { get; set; }

    public int AlertThreshold { get; set; } = 5;

    public string? AlertEmail { get; set; }

    public bool NotifyEmail { get; set; } = true;

    // Automation Fields
    public string? CustomWebhookUrl { get; set; }
    
    public string? TelegramChatId { get; set; }

    public bool NotifyWebhook { get; set; } = false;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
