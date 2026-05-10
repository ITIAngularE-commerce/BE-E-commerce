using ECommerceApi.Services.DTOs.Review;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IReviewService
    {
        Task<List<ReviewDto>> GetProductReviewsAsync(int productId);
        Task<ReviewDto> CreateAsync(int productId, string userId, CreateReviewDto dto);
    }

}
