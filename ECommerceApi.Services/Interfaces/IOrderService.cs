
namespace ECommerceApi.Services.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderDto>> CreateAsync(string userId, CreateOrderDto dto);
        Task<ApiResponse<List<OrderDto>>> GetUserOrdersAsync(string userId);
        Task<ApiResponse<OrderDto>> GetByIdAsync(int id, string userId, string role);
        Task<ApiResponse<bool>> CancelAsync(int id, string userId);
        Task<ApiResponse<bool>> UpdateStatusAsync(int id, string status, string adminId);
        Task<ApiResponse<List<OrderDto>>> GetAllOrdersAsync(string adminId);
    }

}
