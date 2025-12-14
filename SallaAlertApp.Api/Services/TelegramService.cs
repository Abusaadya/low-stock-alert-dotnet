using System.Text;
using System.Text.Json;

namespace SallaAlertApp.Api.Services;

public class TelegramService
{
    private readonly HttpClient _http;
    private readonly string _botToken;
    private const string BaseUrl = "https://api.telegram.org/bot";

    public TelegramService(IConfiguration config)
    {
        _http = new HttpClient();
        _botToken = config["TELEGRAM_BOT_TOKEN"] ?? throw new Exception("TELEGRAM_BOT_TOKEN not set");
    }

    public async Task<bool> SendMessageAsync(string chatId, string text)
    {
        if (string.IsNullOrEmpty(chatId)) return false;

        var url = $"{BaseUrl}{_botToken}/sendMessage";
        var payload = new
        {
            chat_id = chatId,
            text = text,
            parse_mode = "Markdown"
        };

        try
        {
            var response = await _http.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Telegram] Error sending message: {error}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] Exception: {ex.Message}");
            return false;
        }
    }

    // Helper to set webhook (developer utility)
    public async Task SetWebhookAsync(string webhookUrl)
    {
        var url = $"{BaseUrl}{_botToken}/setWebhook?url={webhookUrl}";
        await _http.GetAsync(url);
    }

    public async Task<string?> GetBotUsernameAsync()
    {
        var url = $"{BaseUrl}{_botToken}/getMe";
        try
        {
            var response = await _http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("result", out var result))
                {
                    if (result.TryGetProperty("username", out var username))
                    {
                        return username.GetString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] Error fetching bot info: {ex.Message}");
        }
        return null;
    }
}
