
namespace ECommerceApi.Services.Interfaces
{
    public interface ICartService
    {
        Task<ApiResponse<CartDto>> GetCartAsync(string userId);
        Task<ApiResponse<CartDto>> AddItemAsync(string userId, AddToCartDto dto);
        Task<ApiResponse<CartDto>> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemDto dto);
        Task<ApiResponse<CartDto>> RemoveItemAsync(string userId, int cartItemId);
        Task<ApiResponse<bool>> ClearCartAsync(string userId);
        Task<ApiResponse<int>> GetCartItemCountAsync(string userId);
    }
}
