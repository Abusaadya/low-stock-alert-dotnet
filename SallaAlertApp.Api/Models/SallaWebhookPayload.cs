using System.Text.Json.Serialization;

namespace SallaAlertApp.Api.Models;

public class SallaWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("merchant")]
    public long Merchant { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public SallaWebhookData Data { get; set; } = new();
}

public class SallaWebhookData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public SallaWebhookPrice? Price { get; set; }
    
    [JsonPropertyName("plan_name")]
    public string? PlanName { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("urls")]
    public SallaWebhookUrls? Urls { get; set; }
}

public class SallaWebhookPrice
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "SAR";
}

public class SallaWebhookUrls
{
    [JsonPropertyName("customer")]
    public string Customer { get; set; } = string.Empty;
}
