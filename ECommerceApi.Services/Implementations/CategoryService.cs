using ECommerceApi.Data.Models;
using ECommerceApi.Data;
using ECommerceApi.Services.DTOs.Category;
using ECommerceApi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Services.Implementations
{
    public class CategoryService(AppDbContext db, IImageService imageService) : ICategoryService
    {
        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var all = await db.Categories.Include(c => c.SubCategories).ToListAsync();
            return all.Where(c => c.ParentId == null).Select(MapToDto).ToList();
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            string? imageUrl = null;
            if (dto.Image != null)
                imageUrl = await imageService.UploadAsync(dto.Image, "categories");

            var cat = new Category { Name = dto.Name, ParentId = dto.ParentId, ImageUrl = imageUrl };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            return MapToDto(cat);
        }

        public async Task<CategoryDto?> UpdateAsync(int id, CreateCategoryDto dto)
        {
            var cat = await db.Categories.FindAsync(id);
            if (cat == null) return null;

            cat.Name = dto.Name;
            cat.ParentId = dto.ParentId;
            if (dto.Image != null)
                cat.ImageUrl = await imageService.UploadAsync(dto.Image, "categories");

            await db.SaveChangesAsync();
            return MapToDto(cat);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cat = await db.Categories.FindAsync(id);
            if (cat == null) return false;
            db.Categories.Remove(cat);
            await db.SaveChangesAsync();
            return true;
        }

        private static CategoryDto MapToDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            ImageUrl = c.ImageUrl,
            ParentId = c.ParentId,
            SubCategories = c.SubCategories?.Select(MapToDto).ToList() ?? []
        };
    }
}
