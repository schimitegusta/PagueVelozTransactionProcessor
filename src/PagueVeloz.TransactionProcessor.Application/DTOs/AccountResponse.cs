using System.Text.Json.Serialization;

namespace PagueVeloz.TransactionProcessor.Application.DTOs
{
    public class AccountResponse
    {
        [JsonPropertyName("account_id")]
        public string AccountId { get; set; } = string.Empty;

        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("balance")]
        public long Balance { get; set; }

        [JsonPropertyName("reserved_balance")]
        public long ReservedBalance { get; set; }

        [JsonPropertyName("available_balance")]
        public long AvailableBalance { get; set; }

        [JsonPropertyName("credit_limit")]
        public long CreditLimit { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
