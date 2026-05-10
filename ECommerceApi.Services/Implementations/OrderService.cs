using ECommerceApi.Data.Models;
using ECommerceApi.Data;
using ECommerceApi.Services.DTOs.Order;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace ECommerceApi.Services.Implementations
{
    public class OrderService(AppDbContext db, IEmailService emailService,
        UserManager<ApplicationUser> userManager) : IOrderService
    {
        public async Task<OrderDto> CreateAsync(string userId, CreateOrderDto dto)
        {
            var cart = await db.Carts
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId)
                ?? throw new Exception("Cart is empty");

            if (!cart.Items.Any()) throw new Exception("Cart is empty");

            var address = await db.Addresses.FindAsync(dto.AddressId)
                          ?? throw new Exception("Address not found");

            var paymentMethod = Enum.Parse<PaymentMethod>(dto.PaymentMethod);

            var order = new Order
            {
                UserId = userId,
                AddressId = dto.AddressId,
                PaymentMethod = paymentMethod,
                TrackingCode = Guid.NewGuid().ToString("N")[..10].ToUpper(),
                Status = OrderStatus.Pending
            };

            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product!.Price
                });
                item.Product.Stock -= item.Quantity;
            }

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            db.Orders.Add(order);
            db.CartItems.RemoveRange(cart.Items);
            await db.SaveChangesAsync();

            // إرسال إيميل تأكيد
            var user = await userManager.FindByIdAsync(userId);
            if (user?.Email != null)
                await emailService.SendOrderConfirmationAsync(user.Email, user.FullName, order.Id, order.Total);

            return (await GetByIdAsync(order.Id, userId, "Customer"))!;
        }

        public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
        {
            var orders = await db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Address)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto?> GetByIdAsync(int id, string userId, string role)
        {
            var order = await db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Address)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return null;
            if (role == "Customer" && order.UserId != userId) return null;

            return MapToDto(order);
        }

        public async Task<bool> CancelAsync(int id, string userId)
        {
            var order = await db.Orders.Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null || order.Status != OrderStatus.Pending) return false;

            order.Status = OrderStatus.Cancelled;

            // إرجاع الستوك
            foreach (var item in order.Items)
                if (item.Product != null) item.Product.Stock += item.Quantity;

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var order = await db.Orders.FindAsync(id);
            if (order == null) return false;

            order.Status = Enum.Parse<OrderStatus>(status);
            await db.SaveChangesAsync();
            return true;
        }

        private static OrderDto MapToDto(Order o)
        {
            return new OrderDto
            {
                Id = o.Id,
                Status = o.Status.ToString(),
                PaymentMethod = o.PaymentMethod.ToString(),
                TrackingCode = o.TrackingCode,
                Total = o.Total,
                CreatedAt = o.CreatedAt,
                Address = new AddressSnapshotDto
                {
                    Street = o.Address?.Street ?? "",
                    City = o.Address?.City ?? "",
                    Country = o.Address?.Country ?? ""
                },
                Items = o.Items.Select(i =>
                {
                    var images = string.IsNullOrEmpty(i.Product?.ImageUrls) ? [] :
                        JsonSerializer.Deserialize<List<string>>(i.Product.ImageUrls) ?? [];
                    return new OrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product?.Name ?? "",
                        ImageUrl = images.FirstOrDefault(),
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Subtotal = i.UnitPrice * i.Quantity
                    };
                }).ToList()
            };
        }
    }

}
