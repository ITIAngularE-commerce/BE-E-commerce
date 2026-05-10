using ECommerceApi.Data.Models;
using ECommerceApi.Data;
using ECommerceApi.Services.DTOs.Cart;
using ECommerceApi.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ECommerceApi.Services.Implementations
{
    public class CartService(AppDbContext db) : ICartService
    {
        public async Task<CartDto> GetCartAsync(string userId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            return MapToDto(cart);
        }

        public async Task<CartDto> AddItemAsync(string userId, AddToCartDto dto)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var product = await db.Products.FindAsync(dto.ProductId)
                          ?? throw new Exception("Product not found");

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
            if (existing != null)
                existing.Quantity += dto.Quantity;
            else
                cart.Items.Add(new CartItem { CartId = cart.Id, ProductId = dto.ProductId, Quantity = dto.Quantity });

            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return MapToDto(await GetOrCreateCartAsync(userId));
        }

        public async Task<CartDto> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemDto dto)
        {
            var item = await db.CartItems.FindAsync(cartItemId)
                       ?? throw new Exception("Item not found");

            item.Quantity = dto.Quantity;
            await db.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartDto> RemoveItemAsync(string userId, int cartItemId)
        {
            var item = await db.CartItems.FindAsync(cartItemId);
            if (item != null) { db.CartItems.Remove(item); await db.SaveChangesAsync(); }
            return await GetCartAsync(userId);
        }

        public async Task<bool> ClearCartAsync(string userId)
        {
            var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return false;
            db.CartItems.RemoveRange(cart.Items);
            await db.SaveChangesAsync();
            return true;
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
                var images = string.IsNullOrEmpty(i.Product?.ImageUrls) ? [] :
                    JsonSerializer.Deserialize<List<string>>(i.Product.ImageUrls) ?? [];
                return new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "",
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
