
namespace ECommerceApi.Services.Interfaces
{
    public interface IAdminService
    {
        Task<ApiResponse<List<UserProfileDto>>> GetAllUsersAsync(string? role);
        Task<ApiResponse<bool>> ToggleUserStatusAsync(string userId);
        Task<ApiResponse<List<OrderDto>>> GetAllOrdersAsync();
        Task<ApiResponse<AdminStatsDto>> GetStatsAsync();
    }
}
