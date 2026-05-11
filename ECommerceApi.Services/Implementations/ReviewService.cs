
namespace ECommerceApi.Services.Implementations
{
    public class ReviewService(AppDbContext db, UserManager<ApplicationUser> userManager) : IReviewService
    {
        public async Task<ApiResponse<List<ReviewDto>>> GetProductReviewsAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    return ApiResponse<List<ReviewDto>>.Failure("Invalid product ID");

                var productExists = await db.Products.AnyAsync(p => p.Id == productId && p.IsActive);
                if (!productExists)
                    return ApiResponse<List<ReviewDto>>.Failure($"Product with ID {productId} not found");

                var reviews = await db.Reviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = r.User.FullName,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    }).ToListAsync();

                if (!reviews.Any())
                    return ApiResponse<List<ReviewDto>>.Success(new List<ReviewDto>(), "No reviews found for this product");

                var averageRating = reviews.Average(r => r.Rating);
                return ApiResponse<List<ReviewDto>>.Success(reviews, $"Successfully retrieved {reviews.Count} review(s). Average rating: {averageRating:F1}/5");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ReviewDto>>.Failure($"An error occurred while retrieving reviews: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ReviewDto>> CreateAsync(int productId, string userId, CreateReviewDto dto)
        {
            try
            {
                if (productId <= 0)
                    return ApiResponse<ReviewDto>.Failure("Invalid product ID");

                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<ReviewDto>.Failure("User ID is required");

                if (dto.Rating < 1 || dto.Rating > 5)
                    return ApiResponse<ReviewDto>.Failure("Rating must be between 1 and 5");

                if (string.IsNullOrWhiteSpace(dto.Comment))
                    return ApiResponse<ReviewDto>.Failure("Review comment is required");

                if (dto.Comment.Length < 5)
                    return ApiResponse<ReviewDto>.Failure("Review comment must be at least 5 characters");

                if (dto.Comment.Length > 1000)
                    return ApiResponse<ReviewDto>.Failure("Review comment cannot exceed 1000 characters");

                var product = await db.Products.FindAsync(productId);
                if (product == null)
                    return ApiResponse<ReviewDto>.Failure($"Product with ID {productId} not found");

                if (!product.IsActive)
                    return ApiResponse<ReviewDto>.Failure("Cannot review an inactive product");

                var existing = await db.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

                if (existing != null)
                    return ApiResponse<ReviewDto>.Failure("You have already reviewed this product. You can only review once.");

                var hasPurchased = await db.Orders
                    .AnyAsync(o => o.UserId == userId &&
                                   o.Status == OrderStatus.Delivered &&
                                   o.Items.Any(i => i.ProductId == productId));

                if (!hasPurchased)
                    return ApiResponse<ReviewDto>.Failure("You can only review products you have purchased and received");

                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment.Trim()
                };

                db.Reviews.Add(review);
                await db.SaveChangesAsync();

                var user = await userManager.FindByIdAsync(userId);

                var reviewDto = new ReviewDto
                {
                    Id = review.Id,
                    UserId = userId,
                    UserName = user?.FullName ?? string.Empty,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt
                };

                return ApiResponse<ReviewDto>.Success(reviewDto, "Review added successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ReviewDto>.Failure($"An error occurred while creating review: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ReviewDto>> UpdateAsync(int reviewId, string userId, UpdateReviewDto dto)
        {
            try
            {
                if (reviewId <= 0)
                    return ApiResponse<ReviewDto>.Failure("Invalid review ID");

                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<ReviewDto>.Failure("User ID is required");

                var review = await db.Reviews
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == reviewId);

                if (review == null)
                    return ApiResponse<ReviewDto>.Failure($"Review with ID {reviewId} not found");

                if (review.UserId != userId)
                    return ApiResponse<ReviewDto>.Failure("You don't have permission to update this review");

                if (dto.Rating.HasValue)
                {
                    if (dto.Rating.Value < 1 || dto.Rating.Value > 5)
                        return ApiResponse<ReviewDto>.Failure("Rating must be between 1 and 5");
                    review.Rating = dto.Rating.Value;
                }

                if (!string.IsNullOrWhiteSpace(dto.Comment))
                {
                    if (dto.Comment.Length < 5)
                        return ApiResponse<ReviewDto>.Failure("Review comment must be at least 5 characters");
                    if (dto.Comment.Length > 1000)
                        return ApiResponse<ReviewDto>.Failure("Review comment cannot exceed 1000 characters");
                    review.Comment = dto.Comment.Trim();
                }

                await db.SaveChangesAsync();

                var reviewDto = new ReviewDto
                {
                    Id = review.Id,
                    UserId = review.UserId,
                    UserName = review.User?.FullName ?? string.Empty,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt
                };

                return ApiResponse<ReviewDto>.Success(reviewDto, "Review updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ReviewDto>.Failure($"An error occurred while updating review: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int reviewId, string userId, string role)
        {
            try
            {
                if (reviewId <= 0)
                    return ApiResponse<bool>.Failure("Invalid review ID");

                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                var review = await db.Reviews.FindAsync(reviewId);
                if (review == null)
                    return ApiResponse<bool>.Failure($"Review with ID {reviewId} not found");

                if (role != "Admin" && review.UserId != userId)
                    return ApiResponse<bool>.Failure("You don't have permission to delete this review");

                db.Reviews.Remove(review);
                await db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Review deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while deleting review: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AverageRatingDto>> GetProductRatingAsync(int productId)
        {
            try
            {
                if (productId <= 0)
                    return ApiResponse<AverageRatingDto>.Failure("Invalid product ID");

                var productExists = await db.Products.AnyAsync(p => p.Id == productId);
                if (!productExists)
                    return ApiResponse<AverageRatingDto>.Failure($"Product with ID {productId} not found");

                var reviews = await db.Reviews
                    .Where(r => r.ProductId == productId)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    return ApiResponse<AverageRatingDto>.Success(new AverageRatingDto
                    {
                        AverageRating = 0,
                        TotalReviews = 0,
                        RatingDistribution = new Dictionary<int, int>()
                    }, "No reviews yet");
                }

                var distribution = new Dictionary<int, int>();
                for (int i = 1; i <= 5; i++)
                {
                    distribution[i] = reviews.Count(r => r.Rating == i);
                }

                var ratingDto = new AverageRatingDto
                {
                    AverageRating = reviews.Average(r => r.Rating),
                    TotalReviews = reviews.Count,
                    RatingDistribution = distribution
                };

                return ApiResponse<AverageRatingDto>.Success(ratingDto, $"Average rating: {ratingDto.AverageRating:F1}/5 from {ratingDto.TotalReviews} review(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<AverageRatingDto>.Failure($"An error occurred while retrieving product rating: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ReviewDto>>> GetUserReviewsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<List<ReviewDto>>.Failure("User ID is required");

                var reviews = await db.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserName = r.User.FullName,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        ProductName = r.Product.Name,
                        ProductId = r.ProductId
                    }).ToListAsync();

                if (!reviews.Any())
                    return ApiResponse<List<ReviewDto>>.Success(new List<ReviewDto>(), "No reviews found");

                return ApiResponse<List<ReviewDto>>.Success(reviews, $"Successfully retrieved {reviews.Count} review(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ReviewDto>>.Failure($"An error occurred while retrieving user reviews: {ex.Message}");
            }
        }
    }
}