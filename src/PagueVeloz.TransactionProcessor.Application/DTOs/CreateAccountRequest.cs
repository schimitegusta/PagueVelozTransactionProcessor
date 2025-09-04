using System.Text.Json.Serialization;

namespace PagueVeloz.TransactionProcessor.Application.DTOs
{
    public class CreateAccountRequest
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("initial_balance")]
        public long InitialBalance { get; set; }

        [JsonPropertyName("credit_limit")]
        public long CreditLimit { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "BRL";
    }
}
