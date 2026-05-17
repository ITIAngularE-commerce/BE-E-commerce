
namespace ECommerceApi.Services.DTOs.Payment
{
    public class PaymobSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string HmacSecret { get; set; } = string.Empty;
        public string IframeId { get; set; } = string.Empty;
        public string IntegrationId { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
    }
}
