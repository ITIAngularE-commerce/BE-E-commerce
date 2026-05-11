
namespace ECommerceApi.Services.Implementations
{
    public class ProductService(AppDbContext db, IImageService imageService) : IProductService
    {
        public async Task<ApiResponse<PagedResultDto<ProductDto>>> GetAllAsync(ProductFilterDto f)
        {
            try
            {
                // Validation: Check filter parameters
                if (f.Page <= 0)
                    f.Page = 1;

                if (f.PageSize <= 0 || f.PageSize > 100)
                    f.PageSize = 10;

                if (f.MinPrice.HasValue && f.MinPrice < 0)
                    return ApiResponse<PagedResultDto<ProductDto>>.Failure("Minimum price cannot be negative");

                if (f.MaxPrice.HasValue && f.MaxPrice < 0)
                    return ApiResponse<PagedResultDto<ProductDto>>.Failure("Maximum price cannot be negative");

                if (f.MinPrice.HasValue && f.MaxPrice.HasValue && f.MinPrice > f.MaxPrice)
                    return ApiResponse<PagedResultDto<ProductDto>>.Failure("Minimum price cannot be greater than maximum price");

                var query = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(f.Search))
                {
                    var searchTerm = f.Search.Trim();
                    query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
                }

                // Apply category filter
                if (f.CategoryId.HasValue && f.CategoryId.Value > 0)
                {
                    var categoryExists = await db.Categories.AnyAsync(c => c.Id == f.CategoryId.Value);
                    if (!categoryExists)
                        return ApiResponse<PagedResultDto<ProductDto>>.Failure($"Category with ID {f.CategoryId} not found");

                    query = query.Where(p => p.CategoryId == f.CategoryId);
                }

                // Apply price filters
                if (f.MinPrice.HasValue)
                    query = query.Where(p => p.Price >= f.MinPrice.Value);

                if (f.MaxPrice.HasValue)
                    query = query.Where(p => p.Price <= f.MaxPrice.Value);

                // Apply sorting
                query = f.SortBy?.ToLower() switch
                {
                    "price" => f.Ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                    "rating" => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0),
                    "name" => f.Ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };

                var total = await query.CountAsync();

                var items = await query
                    .Skip((f.Page - 1) * f.PageSize)
                    .Take(f.PageSize)
                    .ToListAsync();

                var result = new PagedResultDto<ProductDto>
                {
                    Items = items.Select(MapToDto).ToList(),
                    TotalCount = total,
                    Page = f.Page,
                    PageSize = f.PageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)f.PageSize)
                };

                var message = total > 0 ? $"Successfully retrieved {items.Count} product(s)" : "No products found";
                return ApiResponse<PagedResultDto<ProductDto>>.Success(result, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResultDto<ProductDto>>.Failure($"An error occurred while retrieving products: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ProductDto>> GetByIdAsync(int id)
        {
            try
            {
                // Validation: Check if ID is valid
                if (id <= 0)
                    return ApiResponse<ProductDto>.Failure("Invalid product ID");

                var product = await db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Seller)
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                    return ApiResponse<ProductDto>.Failure($"Product with ID {id} not found");

                return ApiResponse<ProductDto>.Success(MapToDto(product), "Product retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductDto>.Failure($"An error occurred while retrieving product: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ProductDto>> CreateAsync(string sellerId, CreateProductDto dto)
        {
            try
            {
                // Validation 1: Check seller ID
                if (string.IsNullOrWhiteSpace(sellerId))
                    return ApiResponse<ProductDto>.Failure("Seller ID is required");

                // Validation 2: Check product name
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return ApiResponse<ProductDto>.Failure("Product name is required");

                if (dto.Name.Length < 3)
                    return ApiResponse<ProductDto>.Failure("Product name must be at least 3 characters");

                if (dto.Name.Length > 100)
                    return ApiResponse<ProductDto>.Failure("Product name cannot exceed 100 characters");

                // Validation 3: Check description
                if (string.IsNullOrWhiteSpace(dto.Description))
                    return ApiResponse<ProductDto>.Failure("Product description is required");

                if (dto.Description.Length < 10)
                    return ApiResponse<ProductDto>.Failure("Product description must be at least 10 characters");

                // Validation 4: Check price
                if (dto.Price <= 0)
                    return ApiResponse<ProductDto>.Failure("Product price must be greater than zero");

                if (dto.Price > 1000000)
                    return ApiResponse<ProductDto>.Failure("Product price cannot exceed 1,000,000");

                // Validation 5: Check stock
                if (dto.Stock < 0)
                    return ApiResponse<ProductDto>.Failure("Stock cannot be negative");

                if (dto.Stock > 100000)
                    return ApiResponse<ProductDto>.Failure("Stock cannot exceed 100,000");

                // Validation 6: Check category
                if (dto.CategoryId <= 0)
                    return ApiResponse<ProductDto>.Failure("Valid category ID is required");

                var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId);
                if (!categoryExists)
                    return ApiResponse<ProductDto>.Failure($"Category with ID {dto.CategoryId} not found");

                // Validation 7: Check images
                if (dto.Images == null || !dto.Images.Any())
                    return ApiResponse<ProductDto>.Failure("At least one product image is required");

                if (dto.Images.Count > 10)
                    return ApiResponse<ProductDto>.Failure("Maximum 10 images allowed per product");

                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                foreach (var img in dto.Images)
                {
                    var fileExtension = System.IO.Path.GetExtension(img.FileName).ToLower();
                    if (!validExtensions.Contains(fileExtension))
                        return ApiResponse<ProductDto>.Failure($"Invalid image format '{fileExtension}'. Allowed formats: {string.Join(", ", validExtensions)}");

                    if (img.Length > 5 * 1024 * 1024) // 5MB
                        return ApiResponse<ProductDto>.Failure($"Image '{img.FileName}' exceeds 5MB limit");
                }

                // Upload images
                var imageUrls = new List<string>();
                foreach (var img in dto.Images)
                {
                    try
                    {
                        var url = await imageService.UploadAsync(img, "products");
                        imageUrls.Add(url);
                    }
                    catch (Exception ex)
                    {
                        return ApiResponse<ProductDto>.Failure($"Failed to upload image '{img.FileName}': {ex.Message}");
                    }
                }

                // Create product
                var product = new Product
                {
                    Name = dto.Name.Trim(),
                    Description = dto.Description.Trim(),
                    Price = dto.Price,
                    Stock = dto.Stock,
                    CategoryId = dto.CategoryId,
                    SellerId = sellerId,
                    ImageUrls = JsonSerializer.Serialize(imageUrls),
                    IsActive = true
                };

                db.Products.Add(product);
                await db.SaveChangesAsync();

                var createdProduct = await GetByIdAsync(product.Id);
                return ApiResponse<ProductDto>.Success(createdProduct.Data!, "Product created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductDto>.Failure($"An error occurred while creating product: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ProductDto>> UpdateAsync(int id, string sellerId, UpdateProductDto dto)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<ProductDto>.Failure("Invalid product ID");

                if (string.IsNullOrWhiteSpace(sellerId))
                    return ApiResponse<ProductDto>.Failure("Seller ID is required");

                var product = await db.Products.FindAsync(id);
                if (product == null)
                    return ApiResponse<ProductDto>.Failure($"Product with ID {id} not found");

                if (product.SellerId != sellerId)
                    return ApiResponse<ProductDto>.Failure("You don't have permission to update this product");

                if (!product.IsActive)
                    return ApiResponse<ProductDto>.Failure("Cannot update an inactive product");

                if (dto.Name != null)
                {
                    if (string.IsNullOrWhiteSpace(dto.Name))
                        return ApiResponse<ProductDto>.Failure("Product name cannot be empty");

                    if (dto.Name.Length < 3)
                        return ApiResponse<ProductDto>.Failure("Product name must be at least 3 characters");

                    if (dto.Name.Length > 100)
                        return ApiResponse<ProductDto>.Failure("Product name cannot exceed 100 characters");

                    product.Name = dto.Name.Trim();
                }

                if (dto.Description != null)
                {
                    if (string.IsNullOrWhiteSpace(dto.Description))
                        return ApiResponse<ProductDto>.Failure("Product description cannot be empty");

                    if (dto.Description.Length < 10)
                        return ApiResponse<ProductDto>.Failure("Product description must be at least 10 characters");

                    product.Description = dto.Description.Trim();
                }

                if (dto.Price.HasValue)
                {
                    if (dto.Price.Value <= 0)
                        return ApiResponse<ProductDto>.Failure("Price must be greater than zero");

                    if (dto.Price.Value > 1000000)
                        return ApiResponse<ProductDto>.Failure("Price cannot exceed 1,000,000");

                    product.Price = dto.Price.Value;
                }

                if (dto.Stock.HasValue)
                {
                    if (dto.Stock.Value < 0)
                        return ApiResponse<ProductDto>.Failure("Stock cannot be negative");

                    if (dto.Stock.Value > 100000)
                        return ApiResponse<ProductDto>.Failure("Stock cannot exceed 100,000");

                    product.Stock = dto.Stock.Value;
                }

                if (dto.CategoryId.HasValue)
                {
                    if (dto.CategoryId.Value <= 0)
                        return ApiResponse<ProductDto>.Failure("Valid category ID is required");

                    var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value);
                    if (!categoryExists)
                        return ApiResponse<ProductDto>.Failure($"Category with ID {dto.CategoryId} not found");

                    product.CategoryId = dto.CategoryId.Value;
                }

                if (dto.Images != null && dto.Images.Any())
                {
                    if (dto.Images.Count > 10)
                        return ApiResponse<ProductDto>.Failure("Maximum 10 images allowed per product");

                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    foreach (var img in dto.Images)
                    {
                        var fileExtension = Path.GetExtension(img.FileName).ToLower();
                        if (!validExtensions.Contains(fileExtension))
                            return ApiResponse<ProductDto>.Failure($"Invalid image format '{fileExtension}'. Allowed formats: {string.Join(", ", validExtensions)}");

                        if (img.Length > 5 * 1024 * 1024)
                            return ApiResponse<ProductDto>.Failure($"Image '{img.FileName}' exceeds 5MB limit");
                    }

                    var newImageUrls = new List<string>();
                    foreach (var img in dto.Images)
                    {
                        try
                        {
                            var url = await imageService.UploadAsync(img, "products");
                            newImageUrls.Add(url);
                        }
                        catch (Exception ex)
                        {
                            return ApiResponse<ProductDto>.Failure($"Failed to upload image '{img.FileName}': {ex.Message}");
                        }
                    }

                    var existingImages = string.IsNullOrEmpty(product.ImageUrls)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(product.ImageUrls) ?? new List<string>();

                    if (dto.ReplaceImages)
                    {
                        // Replace all images
                        product.ImageUrls = JsonSerializer.Serialize(newImageUrls);
                    }
                    else
                    {
                        // Append new images
                        existingImages.AddRange(newImageUrls);
                        product.ImageUrls = JsonSerializer.Serialize(existingImages);
                    }
                }
                await db.SaveChangesAsync();

                var updatedProduct = await GetByIdAsync(id);
                return ApiResponse<ProductDto>.Success(updatedProduct.Data!, "Product updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<ProductDto>.Failure($"An error occurred while updating product: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id, string sellerId, string role)
        {
            try
            {
                // Validation 1: Check ID
                if (id <= 0)
                    return ApiResponse<bool>.Failure("Invalid product ID");

                // Validation 2: Check seller ID
                if (string.IsNullOrWhiteSpace(sellerId))
                    return ApiResponse<bool>.Failure("Seller ID is required");

                // Validation 3: Check role
                if (string.IsNullOrWhiteSpace(role))
                    role = "Customer";

                // Validation 4: Find product
                var product = await db.Products.FindAsync(id);
                if (product == null)
                    return ApiResponse<bool>.Failure($"Product with ID {id} not found");

                // Validation 5: Check permission (Admin or owner)
                if (role != "Admin" && product.SellerId != sellerId)
                    return ApiResponse<bool>.Failure("You don't have permission to delete this product");

                // Validation 6: Check if already deleted
                if (!product.IsActive)
                    return ApiResponse<bool>.Failure("Product is already deleted");

                // Soft delete
                product.IsActive = false;
                await db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Product deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while deleting product: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ProductDto>>> GetProductsBySellerAsync(string sellerId)
        {
            try
            {
                // Validation: Check seller ID
                if (string.IsNullOrWhiteSpace(sellerId))
                    return ApiResponse<List<ProductDto>>.Failure("Seller ID is required");

                var products = await db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .Where(p => p.SellerId == sellerId && p.IsActive)
                    .Select(p => MapToDto(p))
                    .ToListAsync();

                var message = products.Any() ? $"Successfully retrieved {products.Count} product(s)" : "No products found for this seller";
                return ApiResponse<List<ProductDto>>.Success(products, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<ProductDto>>.Failure($"An error occurred while retrieving seller products: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateStockAsync(int id, int quantity, string sellerId)
        {
            try
            {
                // Validation 1: Check ID
                if (id <= 0)
                    return ApiResponse<bool>.Failure("Invalid product ID");

                // Validation 2: Check seller ID
                if (string.IsNullOrWhiteSpace(sellerId))
                    return ApiResponse<bool>.Failure("Seller ID is required");

                // Validation 3: Find product
                var product = await db.Products.FindAsync(id);
                if (product == null)
                    return ApiResponse<bool>.Failure($"Product with ID {id} not found");

                // Validation 4: Check ownership
                if (product.SellerId != sellerId)
                    return ApiResponse<bool>.Failure("You don't have permission to update this product's stock");

                // Validation 5: Validate quantity
                var newStock = product.Stock + quantity;
                if (newStock < 0)
                    return ApiResponse<bool>.Failure($"Insufficient stock. Current stock: {product.Stock}, Requested reduction: {-quantity}");

                if (newStock > 100000)
                    return ApiResponse<bool>.Failure($"Stock cannot exceed 100,000. Current stock: {product.Stock}");

                // Update stock
                product.Stock = newStock;
                await db.SaveChangesAsync();

                var action = quantity >= 0 ? "increased by" : "decreased by";
                return ApiResponse<bool>.Success(true, $"Stock {action} {Math.Abs(quantity)}. New stock: {product.Stock}");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while updating stock: {ex.Message}");
            }
        }

        private static ProductDto MapToDto(Product p)
        {
            var images = string.IsNullOrEmpty(p.ImageUrls)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(p.ImageUrls) ?? new List<string>();

            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                ImageUrls = images,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                SellerName = p.Seller?.FullName ?? string.Empty,
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count,
                CreatedAt = p.CreatedAt
            };
        }
    }
}