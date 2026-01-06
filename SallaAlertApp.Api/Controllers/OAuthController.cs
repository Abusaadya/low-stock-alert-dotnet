using Microsoft.AspNetCore.Mvc;

namespace SallaAlertApp.Api.Controllers;

[Route("oauth")]
public class OAuthController : BaseController
{
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(ILogger<OAuthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("callback")]
    public IActionResult Callback([FromQuery] string? merchant)
    {
        // In Easy Mode, the 'app.store.authorize' webhook handles the token exchange.
        // This callback is just the entry point when the user opens the app from Salla.
        
        _logger.LogInformation($"[OAuth] Callback hit. Merchant: {merchant}");

        if (!string.IsNullOrEmpty(merchant))
        {
             return Redirect($"/settings?merchant={merchant}");
        }

        return Redirect("/settings");
    }
}
