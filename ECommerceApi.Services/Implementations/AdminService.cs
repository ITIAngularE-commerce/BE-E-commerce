using ECommerceApi.Data.Models;
using ECommerceApi.Data;
using ECommerceApi.Services.DTOs.Order;
using ECommerceApi.Services.DTOs.User;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApi.Services.Implementations
{
    public class AdminService(UserManager<ApplicationUser> userManager, AppDbContext db,
        IOrderService orderService) : IAdminService
    {
        public async Task<List<UserProfileDto>> GetAllUsersAsync(string? role)
        {
            var users = role != null
                ? (await userManager.GetUsersInRoleAsync(role)).ToList()
                : userManager.Users.ToList();

            var result = new List<UserProfileDto>();
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                result.Add(new UserProfileDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email!,
                    PhoneNumber = u.PhoneNumber ?? "",
                    Role = roles.FirstOrDefault() ?? "Customer",
                    CreatedAt = u.CreatedAt
                });
            }
            return result;
        }

        public async Task<bool> ToggleUserStatusAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            await userManager.UpdateAsync(user);
            return true;
        }

        public async Task<List<OrderDto>> GetAllOrdersAsync()
        {
            return await orderService.GetUserOrdersAsync("__all__")
                .ContinueWith(_ =>
                    db.Orders
                        .Include(o => o.Items).ThenInclude(i => i.Product)
                        .Include(o => o.Address)
                        .OrderByDescending(o => o.CreatedAt)
                        .ToList()
                        .Select(o => orderService.GetByIdAsync(o.Id, "", "Admin").Result!)
                        .Where(o => o != null)
                        .ToList());
        }

        public async Task<AdminStatsDto> GetStatsAsync()
        {
            var revenue = await db.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);

            return new AdminStatsDto
            {
                TotalUsers = await userManager.Users.CountAsync(),
                TotalOrders = await db.Orders.CountAsync(),
                TotalProducts = await db.Products.CountAsync(p => p.IsActive),
                TotalRevenue = revenue
            };
        }
    }

}
