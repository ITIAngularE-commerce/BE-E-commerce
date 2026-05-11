
namespace ECommerceApi.Services.Interfaces
{
    public interface IReviewService
    {
        Task<ApiResponse<List<ReviewDto>>> GetProductReviewsAsync(int productId);
        Task<ApiResponse<ReviewDto>> CreateAsync(int productId, string userId, CreateReviewDto dto);
        Task<ApiResponse<ReviewDto>> UpdateAsync(int reviewId, string userId, UpdateReviewDto dto);
        Task<ApiResponse<bool>> DeleteAsync(int reviewId, string userId, string role);
        Task<ApiResponse<AverageRatingDto>> GetProductRatingAsync(int productId);
        Task<ApiResponse<List<ReviewDto>>> GetUserReviewsAsync(string userId);
    }

}
