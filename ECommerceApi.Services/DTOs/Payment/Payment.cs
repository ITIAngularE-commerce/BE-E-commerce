
namespace ECommerceApi.Services.DTOs.Payment
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }

    public class InitiatePaymentResponse
    {
        public string IframeUrl { get; set; } = string.Empty;
        public string PaymentToken { get; set; } = string.Empty;
    }

    public class PaymobAuthResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class PaymobOrderResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("merchant_order_id")]
        public string MerchantOrderId { get; set; } = string.Empty;
    }

    public class PaymobPaymentKeyResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    public class PaymobCallbackDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("obj")]
        public PaymobTransactionData Obj { get; set; } = new();
    }

    public class PaymobTransactionData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("pending")]
        public bool Pending { get; set; }

        [JsonPropertyName("amount_cents")]
        public long AmountCents { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("merchant_order_id")]
        public string MerchantOrderId { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public PaymobOrderInfo Order { get; set; } = new();
    }

    public class PaymobOrderInfo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("merchant_order_id")]
        public string MerchantOrderId { get; set; } = string.Empty;
    }

}