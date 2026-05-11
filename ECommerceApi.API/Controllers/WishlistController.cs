using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.DTOs.Product;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/wishlist")]
    [Authorize]
    public class WishlistController(IWishlistService wishlistService) : ControllerBase
    {
        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await wishlistService.GetWishlistAsync(userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve wishlist",
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

        [HttpGet("count")]
        public async Task<IActionResult> GetWishlistCount()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await wishlistService.GetWishlistCountAsync(userId);

            if (!result.IsSuccess)
            {
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

        [HttpPost("toggle/{productId}")]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await wishlistService.ToggleAsync(userId, productId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to toggle wishlist",
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

        [HttpGet("check/{productId}")]
        public async Task<IActionResult> IsInWishlist(int productId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await wishlistService.IsInWishlistAsync(userId, productId);

            if (!result.IsSuccess)
            {
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

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearWishlist()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await wishlistService.ClearWishlistAsync(userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to clear wishlist",
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