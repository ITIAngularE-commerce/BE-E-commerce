using ECommerceApi.Services.DTOs.Order;
using ECommerceApi.Services.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IAdminService
    {
        Task<List<UserProfileDto>> GetAllUsersAsync(string? role);
        Task<bool> ToggleUserStatusAsync(string userId);
        Task<List<OrderDto>> GetAllOrdersAsync();
        Task<AdminStatsDto> GetStatsAsync();
    }

    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
