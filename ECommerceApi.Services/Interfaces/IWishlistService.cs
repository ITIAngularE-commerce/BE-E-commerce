using ECommerceApi.Services.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IWishlistService
    {
        Task<List<ProductDto>> GetWishlistAsync(string userId);
        Task<bool> ToggleAsync(string userId, int productId);
    }

}
