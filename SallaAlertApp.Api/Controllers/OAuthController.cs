using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SallaAlertApp.Api.Data;
using SallaAlertApp.Api.Models;
using System.Text.Json;

namespace SallaAlertApp.Api.Controllers;

[Route("oauth")]
public class OAuthController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    public OAuthController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
        _http = new HttpClient();
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var clientId = _config["SALLA_CLIENT_ID"];
        var redirectUri = _config["SALLA_CALLBACK_URL"];
        // Ensure you change this port in launchSettings.json or env to match callback
        var authUrl = _config["SALLA_AUTHORIZATION_URL"];
        var state = Guid.NewGuid().ToString("N");

        var url = $"{authUrl}?client_id={clientId}&response_type=code&redirect_uri={redirectUri}&scope=products.read webhooks.read_write offline_access&state={state}&prompt=login";
        
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest("No code returned");

        try
        {
            // 1. Exchange Code for Token
            var tokenUrl = "https://accounts.salla.sa/oauth2/token";
            var payload = new Dictionary<string, string>
            {
                { "client_id", _config["SALLA_CLIENT_ID"]! },
                { "client_secret", _config["SALLA_CLIENT_SECRET"]! },
                { "grant_type", "authorization_code" },
                { "redirect_uri", _config["SALLA_CALLBACK_URL"]! },
                { "code", code }
            };

            var tokenResponse = await _http.PostAsync(tokenUrl, new FormUrlEncodedContent(payload));
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            
            if (!tokenResponse.IsSuccessStatusCode)
                return BadRequest($"Token Error: {tokenContent}");

            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
            var accessToken = tokenData.GetProperty("access_token").GetString();
            var refreshToken = tokenData.GetProperty("refresh_token").GetString();
            var expiresIn = tokenData.GetProperty("expires_in").GetInt32();

            // 2. Get Merchant Info
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var userRes = await _http.GetAsync("https://accounts.salla.sa/oauth2/user/info");
            var userContent = await userRes.Content.ReadAsStringAsync();
            Console.WriteLine($"[OAuth] Salla User Info JSON: {userContent}");
            
            if (!userRes.IsSuccessStatusCode) return BadRequest("Failed to fetch user info");

            var userData = JsonSerializer.Deserialize<JsonElement>(userContent);
            var merchantId = userData.GetProperty("data").GetProperty("id").GetInt64();
            var merchantName = userData.GetProperty("data").GetProperty("name").GetString();

            // 3. Save/Update DB
            var merchant = await _context.Merchants.FindAsync(merchantId);
            if (merchant == null)
            {
                merchant = new Merchant
                {
                    MerchantId = merchantId,
                    AccessToken = accessToken!,
                    RefreshToken = refreshToken!,
                    ExpiresIn = expiresIn,
                    AlertEmail = _config["EMAIL_USER"], // Default
                    Name = merchantName
                };
                _context.Merchants.Add(merchant);
            }
            else
            {
                merchant.AccessToken = accessToken!;
                merchant.RefreshToken = refreshToken!;
                merchant.ExpiresIn = expiresIn;
                merchant.Name = merchantName;
            }

            await _context.SaveChangesAsync();

            // Create trial subscription for new merchants
            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId);
            
            if (existingSubscription == null)
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

            // Redirect to settings page after successful installation
            return Redirect($"/settings?merchant={merchantId}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}
