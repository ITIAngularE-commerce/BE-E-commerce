
namespace ECommerceApi.Services.Interfaces
{
    public interface IPaymobService
    {
        Task<InitiatePaymentResponse> InitiatePaymentAsync(int orderId, decimal amount, BillingInfo billing);
        bool VerifyHmac(IQueryCollection query, string receivedHmac);
    }
}
