
namespace ECommerceApi.Services.Implementations
{
    public class WishlistService(AppDbContext db) : IWishlistService
    {
        public async Task<ApiResponse<List<ProductDto>>> GetWishlistAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<List<ProductDto>>.Failure("User ID is required");

                var wishlistItems = await db.Wishlists
                    .Where(w => w.UserId == userId)
                    .Include(w => w.Product)
                        .ThenInclude(p => p.Category)
                    .Include(w => w.Product)
                        .ThenInclude(p => p.Seller)
                    .Include(w => w.Product)
                        .ThenInclude(p => p.Reviews)
                    .Select(w => w.Product)
                    .Where(p => p != null && p.IsActive)
                    .ToListAsync();

                if (!wishlistItems.Any())
                    return ApiResponse<List<ProductDto>>.Success(new List<ProductDto>(), "Wishlist is empty");

                var products = wishlistItems.Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    ImageUrls = string.IsNullOrEmpty(p.ImageUrls)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(p.ImageUrls) ?? new List<string>(),
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? string.Empty,
                    SellerName = p.Seller?.FullName ?? string.Empty,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews.Count,
                    CreatedAt = p.CreatedAt
                }).ToList();

                return ApiResponse<List<ProductDto>>.Success(products, $"Successfully retrieved {products.Count} item(s) from wishlist");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ProductDto>>.Failure($"An error occurred while retrieving wishlist: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ToggleAsync(string userId, int productId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                if (productId <= 0)
                    return ApiResponse<bool>.Failure("Invalid product ID");

                var product = await db.Products.FindAsync(productId);
                if (product == null)
                    return ApiResponse<bool>.Failure($"Product with ID {productId} not found");

                if (!product.IsActive)
                    return ApiResponse<bool>.Failure($"Product '{product.Name}' is not available");

                var existing = await db.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existing != null)
                {
                    db.Wishlists.Remove(existing);
                    await db.SaveChangesAsync();
                    return ApiResponse<bool>.Success(false, $"Product '{product.Name}' removed from wishlist");
                }

                db.Wishlists.Add(new Wishlist { UserId = userId, ProductId = productId });
                await db.SaveChangesAsync();
                return ApiResponse<bool>.Success(true, $"Product '{product.Name}' added to wishlist");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while toggling wishlist: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> IsInWishlistAsync(string userId, int productId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                if (productId <= 0)
                    return ApiResponse<bool>.Failure("Invalid product ID");

                var exists = await db.Wishlists
                    .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

                return ApiResponse<bool>.Success(exists, exists ? "Product is in wishlist" : "Product is not in wishlist");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while checking wishlist: {ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> GetWishlistCountAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<int>.Failure("User ID is required");

                var count = await db.Wishlists
                    .Where(w => w.UserId == userId)
                    .CountAsync();

                return ApiResponse<int>.Success(count, $"Wishlist has {count} item(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Failure($"An error occurred while getting wishlist count: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ClearWishlistAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                var items = await db.Wishlists
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                if (!items.Any())
                    return ApiResponse<bool>.Success(true, "Wishlist is already empty");

                db.Wishlists.RemoveRange(items);
                await db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Wishlist cleared successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while clearing wishlist: {ex.Message}");
            }
        }
    }
}