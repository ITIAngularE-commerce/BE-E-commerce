
namespace ECommerceApi.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<List<CategoryDto>>> GetAllAsync();
        Task<ApiResponse<CategoryDto>> GetByIdAsync(int id);
        Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto);
        Task<ApiResponse<CategoryDto>> UpdateAsync(int id, CreateCategoryDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }
}
