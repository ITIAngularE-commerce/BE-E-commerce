using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.DTOs.Order;
using ECommerceApi.Services.DTOs.User;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        // ── GET /api/v1/admin/users ──────────────────────────────
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role)
        {
            var users = await adminService.GetAllUsersAsync(role);
            return Ok(ApiResponse<List<UserProfileDto>>.Ok(users));
        }

        // ── PATCH /api/v1/admin/users/{id}/toggle ────────────────
        [HttpPatch("users/{id}/toggle")]
        public async Task<IActionResult> ToggleUser(string id)
        {
            var result = await adminService.ToggleUserStatusAsync(id);
            if (!result) return NotFound(ApiResponse<string>.Fail("المستخدم غير موجود"));
            return Ok(ApiResponse<string>.Ok("تم تغيير حالة المستخدم"));
        }

        // ── GET /api/v1/admin/orders ─────────────────────────────
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await adminService.GetAllOrdersAsync();
            return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
        }

        // ── GET /api/v1/admin/stats ──────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await adminService.GetStatsAsync();
            return Ok(ApiResponse<AdminStatsDto>.Ok(stats));
        }
    }
}
