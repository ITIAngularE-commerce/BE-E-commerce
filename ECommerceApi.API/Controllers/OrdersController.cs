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
    public class OrderController(IOrderService orderService) : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = "Customer,Seller,Admin")]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await orderService.CreateAsync(userId, dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to create order",
                        errors = result.Errors
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet]
        [Authorize(Roles = "Customer,Seller,Admin")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await orderService.GetUserOrdersAsync(userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve orders",
                        errors = result.Errors
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Customer,Seller,Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await orderService.GetByIdAsync(id, userId, role);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Failed to retrieve order",
                        errors = result.Errors
                    });
                }

                return NotFound(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Customer,Seller,Admin")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await orderService.CancelAsync(id, userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to cancel order",
                        errors = result.Errors
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await orderService.UpdateStatusAsync(id, dto.Status, adminId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to update order status",
                        errors = result.Errors
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await orderService.GetAllOrdersAsync(adminId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve all orders",
                        errors = result.Errors
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("seller")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetSellerOrders()
        {
            var sellerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var result = await orderService.GetSellerOrdersAsync(sellerId);

            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }
    }
}