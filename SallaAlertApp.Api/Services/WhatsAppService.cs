using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace SallaAlertApp.Api.Services;

public class WhatsAppService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;

    public WhatsAppService(IConfiguration config)
    {
        _accountSid = config["TWILIO_ACCOUNT_SID"] ?? throw new Exception("TWILIO_ACCOUNT_SID not set");
        _authToken = config["TWILIO_AUTH_TOKEN"] ?? throw new Exception("TWILIO_AUTH_TOKEN not set");
        _fromNumber = config["TWILIO_WHATSAPP_NUMBER"] ?? "whatsapp:+14155238886"; // Default Sandbox
        
        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<bool> SendWhatsAppMessage(string toNumber, string messageBody)
    {
        try
        {
            // Clean the number: remove spaces, dashes, parentheses
            toNumber = toNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            
            // Ensure it starts with +
            if (!toNumber.StartsWith("+"))
            {
                // If it starts with a country code without +, add it
                if (toNumber.StartsWith("00"))
                {
                    toNumber = "+" + toNumber.Substring(2);
                }
                else if (!toNumber.StartsWith("whatsapp:"))
                {
                    // Assume it's missing the + sign
                    toNumber = "+" + toNumber;
                }
            }
            
            // Ensure number has whatsapp: prefix
            if (!toNumber.StartsWith("whatsapp:"))
            {
                toNumber = $"whatsapp:{toNumber}";
            }

            Console.WriteLine($"[WhatsApp] Sending to: {toNumber}");

            var message = await MessageResource.CreateAsync(
                body: messageBody,
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber(toNumber)
            );

            Console.WriteLine($"[WhatsApp] Message sent: {message.Sid}");
            return message.Status != MessageResource.StatusEnum.Failed;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WhatsApp] Failed to send: {ex.Message}");
            return false;
        }
    }
}
