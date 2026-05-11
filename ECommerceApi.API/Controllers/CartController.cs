using ECommerceApi.Services.DTOs.Cart;
using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/cart")]
    [Authorize]
    public class CartController(ICartService cartService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetCart()
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

            var result = await cartService.GetCartAsync(userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve cart",
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
        public async Task<IActionResult> GetCartItemCount()
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

            var result = await cartService.GetCartItemCountAsync(userId);

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

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
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

            var result = await cartService.AddItemAsync(userId, dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to add item to cart",
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

        [HttpPut("items/{cartItemId}")]
        public async Task<IActionResult> UpdateItem(int cartItemId, [FromBody] UpdateCartItemDto dto)
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

            var result = await cartService.UpdateItemAsync(userId, cartItemId, dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to update cart item",
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

        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
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

            var result = await cartService.RemoveItemAsync(userId, cartItemId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to remove item from cart",
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

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
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

            var result = await cartService.ClearCartAsync(userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to clear cart",
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