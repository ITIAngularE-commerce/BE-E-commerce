using ECommerceApi.Data.Models;
using ECommerceApi.Data;
using System.Text.Json;
using ECommerceApi.Services.DTOs.Product;
using ECommerceApi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Services.Implementations
{
    public class ProductService(AppDbContext db, IImageService imageService) : IProductService
    {
        public async Task<PagedResultDto<ProductDto>> GetAllAsync(ProductFilterDto f)
        {
            var query = db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(f.Search))
                query = query.Where(p => p.Name.Contains(f.Search) || p.Description.Contains(f.Search));

            if (f.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == f.CategoryId);

            if (f.MinPrice.HasValue)
                query = query.Where(p => p.Price >= f.MinPrice);

            if (f.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= f.MaxPrice);

            query = f.SortBy switch
            {
                "price" => f.Ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((f.Page - 1) * f.PageSize)
                .Take(f.PageSize)
                .ToListAsync();

            return new PagedResultDto<ProductDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = f.Page,
                PageSize = f.PageSize
            };
        }

        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var p = await db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            return p == null ? null : MapToDto(p);
        }

        public async Task<ProductDto> CreateAsync(string sellerId, CreateProductDto dto)
        {
            var imageUrls = new List<string>();
            foreach (var img in dto.Images)
                imageUrls.Add(await imageService.UploadAsync(img, "products"));

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId,
                SellerId = sellerId,
                ImageUrls = JsonSerializer.Serialize(imageUrls)
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return (await GetByIdAsync(product.Id))!;
        }

        public async Task<ProductDto?> UpdateAsync(int id, string sellerId, UpdateProductDto dto)
        {
            var product = await db.Products.FindAsync(id);
            if (product == null || product.SellerId != sellerId) return null;

            if (dto.Name != null) product.Name = dto.Name;
            if (dto.Description != null) product.Description = dto.Description;
            if (dto.Price != null) product.Price = dto.Price.Value;
            if (dto.Stock != null) product.Stock = dto.Stock.Value;
            if (dto.CategoryId != null) product.CategoryId = dto.CategoryId.Value;

            await db.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id, string sellerId, string role)
        {
            var product = await db.Products.FindAsync(id);
            if (product == null) return false;
            if (role != "Admin" && product.SellerId != sellerId) return false;

            product.IsActive = false;   // soft delete
            await db.SaveChangesAsync();
            return true;
        }

        private static ProductDto MapToDto(Product p)
        {
            var images = string.IsNullOrEmpty(p.ImageUrls)
                ? []
                : JsonSerializer.Deserialize<List<string>>(p.ImageUrls) ?? [];

            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                ImageUrls = images,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "",
                SellerName = p.Seller?.FullName ?? "",
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count,
                CreatedAt = p.CreatedAt
            };
        }
    }

}
