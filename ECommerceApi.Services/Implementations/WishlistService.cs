using ECommerceApi.Data.Models;
using ECommerceApi.Data;
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
    public class WishlistService(AppDbContext db) : IWishlistService
    {
        public async Task<List<ProductDto>> GetWishlistAsync(string userId)
        {
            return await db.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Product).ThenInclude(p => p.Category)
                .Include(w => w.Product).ThenInclude(p => p.Seller)
                .Include(w => w.Product).ThenInclude(p => p.Reviews)
                .Select(w => w.Product)
                .Where(p => p.IsActive)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    CategoryName = p.Category.Name,
                    SellerName = p.Seller.FullName,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews.Count,
                    CreatedAt = p.CreatedAt
                }).ToListAsync();
        }

        public async Task<bool> ToggleAsync(string userId, int productId)
        {
            var existing = await db.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existing != null)
            {
                db.Wishlists.Remove(existing);
                await db.SaveChangesAsync();
                return false;   // removed
            }

            db.Wishlists.Add(new Wishlist { UserId = userId, ProductId = productId });
            await db.SaveChangesAsync();
            return true;   // added
        }
    }
}
