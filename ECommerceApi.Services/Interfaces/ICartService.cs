using ECommerceApi.Services.DTOs.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(string userId);
        Task<CartDto> AddItemAsync(string userId, AddToCartDto dto);
        Task<CartDto> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemDto dto);
        Task<CartDto> RemoveItemAsync(string userId, int cartItemId);
        Task<bool> ClearCartAsync(string userId);
    }
}
