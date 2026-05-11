
namespace ECommerceApi.Services.Implementations
{
    public class CategoryService(AppDbContext db, IImageService imageService) : ICategoryService
    {
        public async Task<ApiResponse<List<CategoryDto>>> GetAllAsync()
        {
            try
            {
                var allCategories = await db.Categories
                    .Include(c => c.SubCategories)
                    .ToListAsync();

                if (!allCategories.Any())
                {
                    return ApiResponse<List<CategoryDto>>.Success(
                        new List<CategoryDto>(),
                        "No categories found");
                }

                var parentCategories = allCategories
                    .Where(c => c.ParentId == null)
                    .Select(MapToDto)
                    .ToList();

                return ApiResponse<List<CategoryDto>>.Success(
                    parentCategories,
                    $"Successfully retrieved {parentCategories.Count} category(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<CategoryDto>>.Failure(
                    $"An error occurred while retrieving categories: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CategoryDto>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return ApiResponse<CategoryDto>.Failure("Invalid category ID");
                }

                var category = await db.Categories
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return ApiResponse<CategoryDto>.Failure($"Category with ID {id} not found");
                }

                return ApiResponse<CategoryDto>.Success(
                    MapToDto(category),
                    "Category retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CategoryDto>.Failure(
                    $"An error occurred while retrieving category: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto)
        {
            try
            {
                // Validation 1: Check if Name is provided
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ApiResponse<CategoryDto>.Failure("Category name is required");
                }

                // Validation 2: Check if Name already exists (at same parent level)
                var existingCategory = await db.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.ParentId == dto.ParentId);

                if (existingCategory != null)
                {
                    return ApiResponse<CategoryDto>.Failure($"Category '{dto.Name}' already exists at this level");
                }

                // Validation 3: If ParentId is provided, check if parent category exists
                if (dto.ParentId.HasValue)
                {
                    if (dto.ParentId.Value <= 0)
                    {
                        return ApiResponse<CategoryDto>.Failure("Invalid parent category ID");
                    }

                    var parentExists = await db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value);
                    if (!parentExists)
                    {
                        return ApiResponse<CategoryDto>.Failure($"Parent category with ID {dto.ParentId} not found");
                    }
                }

                // Validation 4: Check image size and type (optional but recommended)
                if (dto.Image != null)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    var fileExtension = Path.GetExtension(dto.Image.FileName).ToLower();

                    if (!validExtensions.Contains(fileExtension))
                    {
                        return ApiResponse<CategoryDto>.Failure(
                            $"Invalid image format. Allowed formats: {string.Join(", ", validExtensions)}");
                    }

                    if (dto.Image.Length > 5 * 1024 * 1024) // 5MB
                    {
                        return ApiResponse<CategoryDto>.Failure("Image size must be less than 5MB");
                    }
                }

                // Create category
                string? imageUrl = null;
                if (dto.Image != null)
                {
                    try
                    {
                        imageUrl = await imageService.UploadAsync(dto.Image, "categories");
                    }
                    catch (Exception ex)
                    {
                        return ApiResponse<CategoryDto>.Failure($"Failed to upload image: {ex.Message}");
                    }
                }

                var category = new Category
                {
                    Name = dto.Name.Trim(),
                    ParentId = dto.ParentId,
                    ImageUrl = imageUrl
                };

                db.Categories.Add(category);
                await db.SaveChangesAsync();

                return ApiResponse<CategoryDto>.Success(
                    MapToDto(category),
                    "Category created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CategoryDto>.Failure(
                    $"An error occurred while creating category: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CategoryDto>> UpdateAsync(int id, CreateCategoryDto dto)
        {
            try
            {
                // Validation 1: Check if ID is valid
                if (id <= 0)
                {
                    return ApiResponse<CategoryDto>.Failure("Invalid category ID");
                }

                // Validation 2: Check if Name is provided
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return ApiResponse<CategoryDto>.Failure("Category name is required");
                }

                // Validation 3: Check if category exists
                var category = await db.Categories.FindAsync(id);
                if (category == null)
                {
                    return ApiResponse<CategoryDto>.Failure($"Category with ID {id} not found");
                }

                // Validation 4: Check if ParentId is trying to point to itself
                if (dto.ParentId.HasValue && dto.ParentId.Value == id)
                {
                    return ApiResponse<CategoryDto>.Failure("Category cannot be its own parent");
                }

                // Validation 5: Check if trying to create a circular reference
                if (dto.ParentId.HasValue)
                {
                    if (dto.ParentId.Value <= 0)
                    {
                        return ApiResponse<CategoryDto>.Failure("Invalid parent category ID");
                    }

                    var parentExists = await db.Categories.AnyAsync(c => c.Id == dto.ParentId.Value);
                    if (!parentExists)
                    {
                        return ApiResponse<CategoryDto>.Failure($"Parent category with ID {dto.ParentId} not found");
                    }

                    // Check for circular reference
                    var isCircular = await IsCircularReference(id, dto.ParentId.Value);
                    if (isCircular)
                    {
                        return ApiResponse<CategoryDto>.Failure("Circular reference detected. Cannot set this parent.");
                    }
                }

                // Validation 6: Check if Name already exists at same parent level (excluding current category)
                var existingCategory = await db.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == dto.Name.ToLower()
                        && c.ParentId == dto.ParentId
                        && c.Id != id);

                if (existingCategory != null)
                {
                    return ApiResponse<CategoryDto>.Failure($"Category '{dto.Name}' already exists at this level");
                }

                // Validation 7: Image validation
                if (dto.Image != null)
                {
                    var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    var fileExtension = Path.GetExtension(dto.Image.FileName).ToLower();

                    if (!validExtensions.Contains(fileExtension))
                    {
                        return ApiResponse<CategoryDto>.Failure(
                            $"Invalid image format. Allowed formats: {string.Join(", ", validExtensions)}");
                    }

                    if (dto.Image.Length > 5 * 1024 * 1024) // 5MB
                    {
                        return ApiResponse<CategoryDto>.Failure("Image size must be less than 5MB");
                    }
                }

                // Update category
                category.Name = dto.Name.Trim();
                category.ParentId = dto.ParentId;

                if (dto.Image != null)
                {
                    try
                    {
                        category.ImageUrl = await imageService.UploadAsync(dto.Image, "categories");
                    }
                    catch (Exception ex)
                    {
                        return ApiResponse<CategoryDto>.Failure($"Failed to upload image: {ex.Message}");
                    }
                }

                await db.SaveChangesAsync();

                // Reload with subcategories
                var updatedCategory = await db.Categories
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                return ApiResponse<CategoryDto>.Success(
                    MapToDto(updatedCategory!),
                    "Category updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CategoryDto>.Failure(
                    $"An error occurred while updating category: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                // Validation 1: Check if ID is valid
                if (id <= 0)
                {
                    return ApiResponse<bool>.Failure("Invalid category ID");
                }

                // Validation 2: Check if category exists
                var category = await db.Categories
                    .Include(c => c.SubCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return ApiResponse<bool>.Failure($"Category with ID {id} not found");
                }

                // Validation 3: Check if category has subcategories
                if (category.SubCategories != null && category.SubCategories.Any())
                {
                    var subCategoryNames = string.Join(", ", category.SubCategories.Select(c => c.Name));
                    return ApiResponse<bool>.Failure(
                        $"Cannot delete category '{category.Name}' because it has subcategories: {subCategoryNames}. Delete subcategories first.");
                }

                // Validation 4: Check if category is used in any products
                var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == id);
                if (hasProducts)
                {
                    return ApiResponse<bool>.Failure(
                        $"Cannot delete category '{category.Name}' because it has products assigned to it.");
                }

                // Delete category
                db.Categories.Remove(category);
                await db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Category deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure(
                    $"An error occurred while deleting category: {ex.Message}");
            }
        }
        private async Task<bool> IsCircularReference(int categoryId, int? newParentId)
        {
            if (!newParentId.HasValue || newParentId.Value <= 0)
                return false;

            var currentId = newParentId.Value;
            var visited = new HashSet<int>();

            while (currentId > 0)
            {
                if (currentId == categoryId)
                    return true;

                if (visited.Contains(currentId))
                    return false;

                visited.Add(currentId);

                var category = await db.Categories
                    .Where(c => c.Id == currentId)
                    .Select(c => new { c.ParentId })
                    .FirstOrDefaultAsync();

                if (category == null)
                    break;

                currentId = category.ParentId ?? 0;
            }

            return false;
        }

        private static CategoryDto MapToDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            ImageUrl = c.ImageUrl,
            ParentId = c.ParentId,
            SubCategories = c.SubCategories?.Select(MapToDto).ToList() ?? new List<CategoryDto>()
        };
    }
}