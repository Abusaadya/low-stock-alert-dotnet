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
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPass = _configuration["Email:SmtpPass"];
            var fromEmail = _configuration["Email:FromEmail"] ?? smtpUser;

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                _logger.LogWarning("Email settings are not configured.");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Low Stock Alert", fromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email sent to {to}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email.");
            return false;
        }
    }
}
