using ECommerceApi.Services.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateAsync(string userId, CreateOrderDto dto);
        Task<List<OrderDto>> GetUserOrdersAsync(string userId);
        Task<OrderDto?> GetByIdAsync(int id, string userId, string role);
        Task<bool> CancelAsync(int id, string userId);
        Task<bool> UpdateStatusAsync(int id, string status);
    }

}
