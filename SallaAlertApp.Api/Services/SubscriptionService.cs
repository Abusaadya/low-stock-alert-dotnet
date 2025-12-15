using SallaAlertApp.Api.Data;
using SallaAlertApp.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace SallaAlertApp.Api.Services;

public class SubscriptionService
{
    private readonly ApplicationDbContext _context;

    public SubscriptionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription> CreateTrialSubscription(long merchantId)
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
        return subscription;
    }

    public async Task<bool> CanSendAlert(long merchantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null) return false;

        // Check if trial expired
        if (subscription.Status == SubscriptionStatus.Trial)
        {
            if (subscription.TrialEndsAt < DateTime.UtcNow)
            {
                subscription.Status = SubscriptionStatus.Expired;
                await _context.SaveChangesAsync();
                return false;
            }
        }

        // Check if subscription expired or cancelled
        if (subscription.Status == SubscriptionStatus.Expired || 
            subscription.Status == SubscriptionStatus.Cancelled)
        {
            return false;
        }

        // Reset monthly counter if needed
        if (subscription.LastResetDate.Month != DateTime.UtcNow.Month)
        {
            subscription.AlertsSentThisMonth = 0;
            subscription.LastResetDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // Check alert quota for Free plan
        if (subscription.PlanType == PlanType.Free)
        {
            if (subscription.AlertsSentThisMonth >= subscription.MaxAlertsPerMonth)
            {
                return false; // Quota exceeded
            }
        }

        return true;
    }

    public async Task IncrementAlertCount(long merchantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription != null)
        {
            subscription.AlertsSentThisMonth++;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Subscription?> GetSubscription(long merchantId)
    {
        return await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);
    }

    public async Task UpgradePlan(long merchantId, PlanType newPlan)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null) return;

        subscription.PlanType = newPlan;
        subscription.Status = SubscriptionStatus.Active;
        subscription.StartDate = DateTime.UtcNow;
        subscription.EndDate = newPlan == PlanType.Basic 
            ? DateTime.UtcNow.AddMonths(1) 
            : DateTime.UtcNow.AddYears(1);

        // Update limits based on plan
        switch (newPlan)
        {
            case PlanType.Basic:
                subscription.MaxTelegramAccounts = 2;
                subscription.MaxAlertsPerMonth = 500;
                break;
            case PlanType.Pro:
                subscription.MaxTelegramAccounts = int.MaxValue; // Unlimited
                subscription.MaxAlertsPerMonth = int.MaxValue; // Unlimited
                break;
        }

        subscription.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RenewSubscription(long merchantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null) return;

        subscription.Status = SubscriptionStatus.Active;
        subscription.EndDate = subscription.PlanType == PlanType.Basic
            ? DateTime.UtcNow.AddMonths(1)
            : DateTime.UtcNow.AddYears(1);
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task CancelSubscription(long merchantId)
    {
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.MerchantId == merchantId);

        if (subscription == null) return;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
