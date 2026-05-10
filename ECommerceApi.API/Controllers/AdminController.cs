using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? role)
        {
            // Controller-level validation
            if (!string.IsNullOrEmpty(role))
            {
                var validRoles = new[] { "Admin", "Seller", "Customer" };
                if (!validRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid role parameter. Valid values: {string.Join(", ", validRoles)}"
                    });
                }
            }

            var result = await adminService.GetAllUsersAsync(role);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve users",
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

        [HttpPatch("users/{id}/toggle")]
        public async Task<IActionResult> ToggleUser(string id)
        {
            // Controller-level validation
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "User ID is required"
                });
            }

            if (!Guid.TryParse(id, out _))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid user ID format"
                });
            }

            // Prevent admin from deactivating themselves
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == id)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "You cannot deactivate your own account"
                });
            }

            var result = await adminService.ToggleUserStatusAsync(id);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to toggle user status",
                        errors = result.Errors
                    });
                }

                return NotFound(new
                {
                    success = false,
                    message = result.ErrorMessage ?? "User not found"
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            var result = await adminService.GetAllOrdersAsync();

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

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var result = await adminService.GetStatsAsync();

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve statistics",
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
    }
}