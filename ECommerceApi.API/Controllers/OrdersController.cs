using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.DTOs.Order;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/orders")]
    [Authorize]
    public class OrdersController(IOrderService orderService) : ControllerBase
    {
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "Customer";

        // ── POST /api/v1/orders ──────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderDto dto)
        {
            try
            {
                var order = await orderService.CreateAsync(UserId, dto);
                return CreatedAtAction(nameof(GetById), new { id = order.Id },
                    ApiResponse<OrderDto>.Ok(order, "تم إنشاء الطلب بنجاح"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.Fail(ex.Message));
            }
        }

        // ── GET /api/v1/orders ───────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var orders = await orderService.GetUserOrdersAsync(UserId);
            return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
        }

        // ── GET /api/v1/orders/{id} ──────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var order = await orderService.GetByIdAsync(id, UserId, Role);
            if (order == null) return NotFound(ApiResponse<string>.Fail("الطلب غير موجود"));
            return Ok(ApiResponse<OrderDto>.Ok(order));
        }

        // ── PATCH /api/v1/orders/{id}/cancel ─────────────────────
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await orderService.CancelAsync(id, UserId);
            if (!result) return BadRequest(ApiResponse<string>.Fail("لا يمكن إلغاء هذا الطلب"));
            return Ok(ApiResponse<string>.Ok("تم إلغاء الطلب"));
        }

        // ── PATCH /api/v1/orders/{id}/status ─────────────────────
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Seller")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            var result = await orderService.UpdateStatusAsync(id, dto.Status);
            if (!result) return NotFound(ApiResponse<string>.Fail("الطلب غير موجود"));
            return Ok(ApiResponse<string>.Ok("تم تحديث حالة الطلب"));
        }
    }
}
