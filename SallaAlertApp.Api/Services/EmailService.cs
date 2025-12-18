using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SallaAlertApp.Api.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        // 1. Get configs with fallbacks matches the structure of the provided working snippet
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var rawPort = _configuration["Email:SmtpPort"];
        int smtpPort = 587; // Default to 587 as per user snippet

        if (!string.IsNullOrEmpty(rawPort) && int.TryParse(rawPort, out int parsedPort))
        {
            smtpPort = parsedPort;
        }

        // Allow environment variables (Priority)
        var smtpUser = _configuration["EMAIL_USER"] ?? _configuration["Email:SmtpUser"];
        var smtpPass = _configuration["EMAIL_PASS"] ?? _configuration["Email:SmtpPass"];
        var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;

        // Logging for visibility
        _logger.LogInformation($"Preparing to send email via {smtpHost}:{smtpPort} (User: {smtpUser})");

        var message = new MimeMessage();
        
        // Handle From Address
        if (fromEmail != null && fromEmail.Contains("<"))
        {
            try { message.From.Add(MailboxAddress.Parse(fromEmail)); }
            catch { message.From.Add(new MailboxAddress("Alerts", smtpUser)); }
        }
        else
        {
            message.From.Add(new MailboxAddress("Alerts", fromEmail ?? smtpUser));
        }

        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            using (var client = new SmtpClient())
            {
                client.Timeout = 10000; // Match user snippet timeout

                // Determine Security Options
                var socketOptions = MailKit.Security.SecureSocketOptions.Auto;
                if (smtpPort == 587) socketOptions = MailKit.Security.SecureSocketOptions.StartTls;
                if (smtpPort == 465) socketOptions = MailKit.Security.SecureSocketOptions.SslOnConnect;

                _logger.LogInformation($"Connecting to {smtpHost}:{smtpPort} with {socketOptions}...");

                await client.ConnectAsync(smtpHost, smtpPort, socketOptions);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }

            _logger.LogInformation($"Email sent successfully to {to}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email via {smtpHost}:{smtpPort}. Error: {ex.Message}");
            return false;
        }
    }
}
