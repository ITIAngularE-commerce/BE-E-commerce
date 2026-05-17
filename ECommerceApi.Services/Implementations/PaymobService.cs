
namespace ECommerceApi.Services.Implementations
{
    public class PaymobService : IPaymobService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PaymobSettings _settings;
        private readonly ILogger<PaymobService> _logger;

        public PaymobService(
            IHttpClientFactory httpClientFactory,
            IOptions<PaymobSettings> settings,
            ILogger<PaymobService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        private async Task<string> AuthenticateAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var body = new { api_key = _settings.ApiKey };

                var fullUrl = $"{_settings.BaseUrl}/auth/tokens";
                _logger.LogInformation("Authenticating with Paymob at: {Url}", fullUrl);

                var response = await client.PostAsJsonAsync(fullUrl, body);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Auth failed: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new Exception($"HTTP {response.StatusCode}: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<PaymobAuthResponse>();

                if (result?.Token == null)
                {
                    throw new Exception("No token received from Paymob");
                }

                _logger.LogInformation("Paymob authentication successful");
                return result.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paymob authentication failed");
                throw new Exception($"Paymob authentication failed: {ex.Message}");
            }
        }

        private async Task<long> CreateOrderAsync(string authToken, int orderId, decimal amount)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var amountCents = (long)(amount * 100);

                var body = new
                {
                    auth_token = authToken,
                    delivery_needed = false,
                    amount_cents = amountCents,
                    currency = "EGP",
                    merchant_order_id = orderId.ToString(),
                    items = Array.Empty<object>()
                };

                var fullUrl = $"{_settings.BaseUrl}/ecommerce/orders";

                var response = await client.PostAsJsonAsync(fullUrl, body);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {response.StatusCode}: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<PaymobOrderResponse>();

                return result!.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paymob order creation failed");
                throw;
            }
        }

        private async Task<string> GetPaymentKeyAsync(string authToken, long paymobOrderId, decimal amount, BillingInfo billing)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var amountCents = (long)(amount * 100);

                var body = new
                {
                    auth_token = authToken,
                    amount_cents = amountCents,
                    expiration = 3600,
                    order_id = paymobOrderId,
                    currency = "EGP",
                    integration_id = int.Parse(_settings.IntegrationId),
                    billing_data = new
                    {
                        first_name = billing.FirstName,
                        last_name = billing.LastName,
                        email = billing.Email,
                        phone_number = billing.PhoneNumber,
                        apartment = "NA",
                        floor = "NA",
                        street = "NA",
                        building = "NA",
                        shipping_method = "NA",
                        postal_code = "NA",
                        city = "NA",
                        country = "EG",
                        state = "NA"
                    }
                };

                var fullUrl = $"{_settings.BaseUrl}/acceptance/payment_keys";

                var response = await client.PostAsJsonAsync(fullUrl, body);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"HTTP {response.StatusCode}: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<PaymobPaymentKeyResponse>();

                return result!.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paymob payment key creation failed");
                throw;
            }
        }

        public async Task<InitiatePaymentResponse> InitiatePaymentAsync(int orderId, decimal amount, BillingInfo billing)
        {
            try
            {
                _logger.LogInformation("Initiating Paymob payment for order {OrderId}", orderId);

                var authToken = await AuthenticateAsync();
                var paymobOrderId = await CreateOrderAsync(authToken, orderId, amount);
                var paymentKey = await GetPaymentKeyAsync(authToken, paymobOrderId, amount, billing);

                var iframeUrl = $"https://accept.paymob.com/api/acceptance/iframes/{_settings.IframeId}?payment_token={paymentKey}";

                return new InitiatePaymentResponse
                {
                    IframeUrl = iframeUrl,
                    PaymentToken = paymentKey
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate Paymob payment for order {OrderId}", orderId);
                throw;
            }
        }

        public bool VerifyHmac(IQueryCollection query, string receivedHmac)
        {
            try
            {
                var hmacKeys = new[]
                {
            "amount_cents",
            "created_at",
            "currency",
            "error_occured",
            "has_parent_transaction",
            "id",
            "integration_id",
            "is_3d_secure",
            "is_auth",
            "is_capture",
            "is_refunded",
            "is_standalone_payment",
            "is_voided",
            "order.id",
            "owner",
            "pending",
            "source_data.pan",
            "source_data.sub_type",
            "source_data.type",
            "success"
        };

                var hmacValues = new List<string>();
                foreach (var key in hmacKeys)
                {
                    var queryKey = key.Replace(".", "_");
                    var value = query.ContainsKey(queryKey) ? query[queryKey].ToString() : "";
                    hmacValues.Add(value);
                }

                var concatenatedString = string.Concat(hmacValues);

                using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.HmacSecret));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concatenatedString));
                var computedHmac = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                _logger.LogInformation("Received HMAC: {Received}", receivedHmac);
                _logger.LogInformation("Computed HMAC: {Computed}", computedHmac);
                _logger.LogInformation("Concatenated string: {String}", concatenatedString);

                return computedHmac == receivedHmac.ToLower();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HMAC verification failed");
                return false;
            }
        }
    }
}