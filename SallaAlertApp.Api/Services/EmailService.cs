using System.Net;
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

        // Try primary port
        bool success = await TrySendAsync(smtpHost, smtpPort, smtpUser, smtpPass, message);
        
        // If failed and was using 587, RETRY with 465 (Auto-fallback)
        if (!success && smtpPort == 587 && smtpHost.Contains("gmail", StringComparison.OrdinalIgnoreCase))
        {
             _logger.LogWarning("Connection to 587 failed. Retrying with port 465 (SSL)...");
             success = await TrySendAsync(smtpHost, 465, smtpUser, smtpPass, message);
        }

        return success;
    }

    private async Task<bool> TrySendAsync(string host, int port, string user, string pass, MimeMessage message)
    {
        try
        {
            // FORCE IPv4: Resolve DNS manually to avoid IPv6 timeouts in cloud environments (Railway/Docker)
            // MailKit sometimes defaults to IPv6 which might be blocked or unstable.
            var addresses = await Dns.GetHostAddressesAsync(host);
            var ipAddress = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            
            // If no IPv4 found (unlikely for Gmail), fallback to host string
            var hostOrIp = ipAddress?.ToString() ?? host;
            if (ipAddress != null) _logger.LogInformation($"Resolved {host} to IPv4: {ipAddress}");

            using (var client = new SmtpClient())
            {
                client.Timeout = 20000; // 20 seconds is reasonable
                client.CheckCertificateRevocation = false; // Prevent CRL check hangs

                var socketOptions = MailKit.Security.SecureSocketOptions.Auto;
                if (port == 587) socketOptions = MailKit.Security.SecureSocketOptions.StartTls;
                if (port == 465) socketOptions = MailKit.Security.SecureSocketOptions.SslOnConnect;

                _logger.LogInformation($"Connecting to {hostOrIp}:{port} with {socketOptions}...");

                await client.ConnectAsync(hostOrIp, port, socketOptions);
                await client.AuthenticateAsync(user, pass);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            
            _logger.LogInformation($"Email sent successfully via {host}:{port}");
            return true;
        }
        catch (Exception ex)
        {
             _logger.LogError($"Failed to send via {host}:{port}. Error: {ex.Message}");
             return false;
        }
    }
}