using ECommerceApi.Data.Models;
using ECommerceApi.Data;
using ECommerceApi.Services.DTOs.Review;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;


namespace ECommerceApi.Services.Implementations
{
    public class ReviewService(AppDbContext db, UserManager<ApplicationUser> userManager) : IReviewService
    {
        public async Task<List<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            return await db.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.User.FullName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();
        }

        public async Task<ReviewDto> CreateAsync(int productId, string userId, CreateReviewDto dto)
        {
            var existing = await db.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existing != null) throw new Exception("Already reviewed");

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            db.Reviews.Add(review);
            await db.SaveChangesAsync();

            var user = await userManager.FindByIdAsync(userId);
            return new ReviewDto
            {
                Id = review.Id,
                UserName = user?.FullName ?? "",
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
