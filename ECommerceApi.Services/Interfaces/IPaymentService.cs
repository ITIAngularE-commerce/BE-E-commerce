
namespace ECommerceApi.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<InitiatePaymentResponse>> InitiatePaymobAsync(int orderId, string userId);
        Task<ApiResponse<bool>> HandleCallbackAsync(PaymobCallbackDto callback);
        Task<ApiResponse<PaymentDto>> GetPaymentStatusAsync(int orderId, string userId);
    }
}
