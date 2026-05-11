
namespace ECommerceApi.Services.Interfaces
{
    public interface IProductService
    {
        Task<ApiResponse<PagedResultDto<ProductDto>>> GetAllAsync(ProductFilterDto f);
        Task<ApiResponse<ProductDto>> GetByIdAsync(int id);
        Task<ApiResponse<ProductDto>> CreateAsync(string sellerId, CreateProductDto dto);
        Task<ApiResponse<ProductDto>> UpdateAsync(int id, string sellerId, UpdateProductDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id, string sellerId, string role);
        Task<ApiResponse<List<ProductDto>>> GetProductsBySellerAsync(string sellerId);
        Task<ApiResponse<bool>> UpdateStockAsync(int id, int quantity, string sellerId);
    }
}
