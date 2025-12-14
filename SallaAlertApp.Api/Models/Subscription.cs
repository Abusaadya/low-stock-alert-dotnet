namespace SallaAlertApp.Api.Models;

public enum PlanType
{
    Free = 0,      // Trial - 7 days
    Basic = 1,     // 29 SAR/month
    Pro = 2        // 299 SAR/year
}

public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3
}

public class Subscription
{
    public int Id { get; set; }
    public long MerchantId { get; set; }
    public PlanType PlanType { get; set; }
    public SubscriptionStatus Status { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    
    // Plan Limits
    public int MaxTelegramAccounts { get; set; }
    public int MaxAlertsPerMonth { get; set; }
    
    // Usage Tracking
    public int AlertsSentThisMonth { get; set; }
    public DateTime LastResetDate { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public Merchant? Merchant { get; set; }
}
