using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SallaAlertApp.Api.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    // بنضيف هنا HttpClient عشان هو اللي هيبعت الطلب لموقع Resend
    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            // 1. هات الـ API Key اللي انت خدته من موقع Resend
            // أنصحك تطلعه في متغيرات Railway وتسميه RESEND_API_KEY
            var apiKey = _configuration["RESEND_API_KEY"] ?? "re_123456789";

            _logger.LogInformation("🔄 محاولة إرسال إيميل عبر Resend API إلى: {To}", to);

            // 2. تجهيز البيانات اللي هنبعتها لـ Resend
            var emailData = new
            {
                from = "Salla Alerts <onboarding@resend.dev>", // سيب ده زي ما هو دلوقتي للتجربة
                to = new[] { to },
                subject = subject,
                html = body
            };

            // 3. تحويل البيانات لشكل يفهمه الموقع (JSON)
            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 4. وضع الـ Key في عنوان الطلب للأمان
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // 5. إرسال الطلب الفعلي للموقع
            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ تم إرسال الإيميل بنجاح واختفى الـ Timeout!");
                return true;
            }

            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("❌ فشل إرسال الإيميل. السبب: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 حدث خطأ غير متوقع أثناء الإرسال");
            return false;
        }
    }
}