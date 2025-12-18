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
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        int smtpPort = 587; // Default
            
        try
        {
            // SMART PORT LOGIC v2:
            // 1. Get raw config/env value
            var rawPort = _configuration["Email:SmtpPort"];

            // 2. Parse raw value if exists
            if (!string.IsNullOrEmpty(rawPort) && int.TryParse(rawPort, out int parsedPort))
            {
               smtpPort = parsedPort;
            }

            // 3. Force 465 for Gmail if it was defaulting to 587 (or explicitly set to 587)
            if (smtpHost.Contains("gmail", StringComparison.OrdinalIgnoreCase) && smtpPort == 587)
            {
                smtpPort = 465;
            }
            
            // Allow environment variables to override or serve as primary
            var smtpUser = _configuration["EMAIL_USER"] ?? _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["EMAIL_PASS"] ?? _configuration["Email:SmtpPass"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                _logger.LogWarning("Email settings are not configured.");
                return false;
            }

            var message = new MimeMessage();
            
            // Handle "Name <email>" format if present in fromEmail
            if (fromEmail.Contains("<") && fromEmail.Contains(">"))
            {
               try 
               {
                   message.From.Add(MailboxAddress.Parse(fromEmail)); 
               }
               catch 
               {
                   // Fallback if parse fails
                   message.From.Add(new MailboxAddress("Low Stock Alert", smtpUser));
               }
            }
            else
            {
                message.From.Add(new MailboxAddress("Low Stock Alert", fromEmail));
            }
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // detailed log
            _logger.LogInformation($"Connecting to SMTP: {smtpHost}:{smtpPort}");

            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.Auto);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email sent to {to}");
            return true;
        }
        catch (Exception ex)
        {
            // Log actual variables used, NOT re-read from config
            _logger.LogError(ex, $"Failed to send email. Host: {smtpHost}, Port: {smtpPort}");
            return false;
        }
    }
}
