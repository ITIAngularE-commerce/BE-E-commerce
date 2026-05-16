
namespace ECommerceApi.Services.Interfaces
{
    public interface IEmailService
    {
        Task<ApiResponse<bool>> SendEmailConfirmationAsync(string email, string fullName, string confirmationLink);
        Task<ApiResponse<bool>> SendWelcomeEmailAsync(string email, string fullName);
        Task<ApiResponse<bool>> SendOrderConfirmationAsync(string email, string fullName, int orderId, decimal total);
        Task<ApiResponse<bool>> SendOrderStatusUpdateAsync(string email, string fullName, int orderId, string oldStatus, string newStatus);
        Task<ApiResponse<bool>> SendPaymentConfirmationAsync(string email, string fullName, int orderId, decimal amount);
        Task<ApiResponse<bool>> SendPasswordResetAsync(string email, string fullName, string resetLink);
    }
}
