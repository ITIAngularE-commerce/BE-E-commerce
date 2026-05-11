
namespace ECommerceApi.Services.Implementations
{
    public class CartService(AppDbContext db) : ICartService
    {
        public async Task<ApiResponse<CartDto>> GetCartAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<CartDto>.Failure("User ID is required");

                var cart = await GetOrCreateCartAsync(userId);
                return ApiResponse<CartDto>.Success(MapToDto(cart), "Cart retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CartDto>.Failure($"An error occurred while retrieving cart: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CartDto>> AddItemAsync(string userId, AddToCartDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<CartDto>.Failure("User ID is required");

                if (dto.ProductId <= 0)
                    return ApiResponse<CartDto>.Failure("Invalid product ID");

                if (dto.Quantity <= 0)
                    return ApiResponse<CartDto>.Failure("Quantity must be greater than zero");

                if (dto.Quantity > 100)
                    return ApiResponse<CartDto>.Failure("Maximum quantity per item is 100");

                var product = await db.Products.FindAsync(dto.ProductId);
                if (product == null)
                    return ApiResponse<CartDto>.Failure($"Product with ID {dto.ProductId} not found");

                if (!product.IsActive)
                    return ApiResponse<CartDto>.Failure($"Product '{product.Name}' is not available");

                if (product.Stock < dto.Quantity)
                    return ApiResponse<CartDto>.Failure($"Insufficient stock for '{product.Name}'. Available: {product.Stock}, Requested: {dto.Quantity}");

                var cart = await GetOrCreateCartAsync(userId);

                var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
                if (existing != null)
                {
                    var newQuantity = existing.Quantity + dto.Quantity;
                    if (product.Stock < newQuantity)
                        return ApiResponse<CartDto>.Failure($"Insufficient stock for '{product.Name}'. Available: {product.Stock}, Total in cart would be: {newQuantity}");

                    existing.Quantity = newQuantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity
                    });
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                var updatedCart = await GetOrCreateCartAsync(userId);
                return ApiResponse<CartDto>.Success(MapToDto(updatedCart), "Item added to cart successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CartDto>.Failure($"An error occurred while adding item to cart: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CartDto>> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<CartDto>.Failure("User ID is required");

                if (cartItemId <= 0)
                    return ApiResponse<CartDto>.Failure("Invalid cart item ID");

                if (dto.Quantity <= 0)
                    return ApiResponse<CartDto>.Failure("Quantity must be greater than zero");

                if (dto.Quantity > 100)
                    return ApiResponse<CartDto>.Failure("Maximum quantity per item is 100");

                var item = await db.CartItems
                    .Include(i => i.Product)
                    .Include(i => i.Cart)
                    .FirstOrDefaultAsync(i => i.Id == cartItemId);

                if (item == null)
                    return ApiResponse<CartDto>.Failure("Cart item not found");

                if (item.Cart?.UserId != userId)
                    return ApiResponse<CartDto>.Failure("You don't have permission to update this cart item");

                if (item.Product == null)
                    return ApiResponse<CartDto>.Failure("Product not found");

                if (!item.Product.IsActive)
                    return ApiResponse<CartDto>.Failure($"Product '{item.Product.Name}' is no longer available");

                if (item.Product.Stock < dto.Quantity)
                    return ApiResponse<CartDto>.Failure($"Insufficient stock for '{item.Product.Name}'. Available: {item.Product.Stock}, Requested: {dto.Quantity}");

                item.Quantity = dto.Quantity;
                await db.SaveChangesAsync();

                var cart = await GetOrCreateCartAsync(userId);
                return ApiResponse<CartDto>.Success(MapToDto(cart), "Cart updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CartDto>.Failure($"An error occurred while updating cart: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CartDto>> RemoveItemAsync(string userId, int cartItemId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<CartDto>.Failure("User ID is required");

                if (cartItemId <= 0)
                    return ApiResponse<CartDto>.Failure("Invalid cart item ID");

                var item = await db.CartItems
                    .Include(i => i.Cart)
                    .FirstOrDefaultAsync(i => i.Id == cartItemId);

                if (item == null)
                    return ApiResponse<CartDto>.Failure("Cart item not found");

                if (item.Cart?.UserId != userId)
                    return ApiResponse<CartDto>.Failure("You don't have permission to remove this cart item");

                db.CartItems.Remove(item);
                await db.SaveChangesAsync();

                var cart = await GetOrCreateCartAsync(userId);
                return ApiResponse<CartDto>.Success(MapToDto(cart), "Item removed from cart successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CartDto>.Failure($"An error occurred while removing item from cart: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ClearCartAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                var cart = await db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                    return ApiResponse<bool>.Success(true, "Cart is already empty");

                db.CartItems.RemoveRange(cart.Items);
                await db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Cart cleared successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while clearing cart: {ex.Message}");
            }
        }

        public async Task<ApiResponse<int>> GetCartItemCountAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<int>.Failure("User ID is required");

                var cart = await db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                    return ApiResponse<int>.Success(0, "Cart is empty");

                var itemCount = cart.Items.Sum(i => i.Quantity);
                return ApiResponse<int>.Success(itemCount, $"Cart has {itemCount} item(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.Failure($"An error occurred while getting cart item count: {ex.Message}");
            }
        }

        private async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            var cart = await db.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null) return cart;

            cart = new Cart { UserId = userId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync();
            return cart;
        }

        private static CartDto MapToDto(Cart cart)
        {
            var items = cart.Items.Select(i =>
            {
                var images = string.IsNullOrEmpty(i.Product?.ImageUrls)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(i.Product.ImageUrls) ?? new List<string>();

                return new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ImageUrl = images.FirstOrDefault(),
                    UnitPrice = i.Product?.Price ?? 0,
                    Quantity = i.Quantity,
                    Subtotal = (i.Product?.Price ?? 0) * i.Quantity
                };
            }).ToList();

            return new CartDto
            {
                Id = cart.Id,
                Items = items,
                Total = items.Sum(i => i.Subtotal)
            };
        }
    }
}