
namespace ECommerceApi.Services.Interfaces
{
    public interface IWishlistService
    {
        Task<ApiResponse<List<ProductDto>>> GetWishlistAsync(string userId);
        Task<ApiResponse<bool>> ToggleAsync(string userId, int productId);
        Task<ApiResponse<bool>> IsInWishlistAsync(string userId, int productId);
        Task<ApiResponse<int>> GetWishlistCountAsync(string userId);
        Task<ApiResponse<bool>> ClearWishlistAsync(string userId);
    }

}
