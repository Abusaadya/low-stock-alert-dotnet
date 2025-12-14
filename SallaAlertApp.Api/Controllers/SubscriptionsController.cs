using Microsoft.AspNetCore.Mvc;
using SallaAlertApp.Api.Services;
using SallaAlertApp.Api.Models;

namespace SallaAlertApp.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionsController(SubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription([FromQuery] long merchantId)
    {
        if (merchantId == 0)
        {
            return BadRequest("Merchant ID is required.");
        }

        var subscription = await _subscriptionService.GetSubscription(merchantId);

        if (subscription == null)
        {
            return NotFound("No subscription found for this merchant.");
        }

        return Ok(new
        {
            subscription.PlanType,
            subscription.Status,
            subscription.TrialEndsAt,
            subscription.EndDate,
            subscription.MaxTelegramAccounts,
            subscription.MaxAlertsPerMonth,
            subscription.AlertsSentThisMonth,
            RemainingAlerts = subscription.MaxAlertsPerMonth - subscription.AlertsSentThisMonth,
            IsActive = subscription.Status == SubscriptionStatus.Active || subscription.Status == SubscriptionStatus.Trial
        });
    }
}
