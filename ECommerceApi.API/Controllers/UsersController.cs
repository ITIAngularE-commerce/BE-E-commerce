using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.DTOs.User;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/user")]
    [Authorize]
    public class UserController(IUserService userService) : ControllerBase
    {
        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
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

            var result = await userService.GetProfileAsync(userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve profile",
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

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
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

            var result = await userService.UpdateProfileAsync(userId, dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to update profile",
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

        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses()
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

            var result = await userService.GetAddressesAsync(userId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to retrieve addresses",
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

        [HttpPost("addresses")]
        public async Task<IActionResult> AddAddress([FromBody] CreateAddressDto dto)
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

            var result = await userService.AddAddressAsync(userId, dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to add address",
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

        [HttpPut("addresses/{addressId}")]
        public async Task<IActionResult> UpdateAddress(int addressId, [FromBody] CreateAddressDto dto)
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

            var result = await userService.UpdateAddressAsync(userId, addressId, dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to update address",
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

        [HttpDelete("addresses/{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
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

            var result = await userService.DeleteAddressAsync(userId, addressId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to delete address",
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

        [HttpPatch("addresses/{addressId}/default")]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
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

            var result = await userService.SetDefaultAddressAsync(userId, addressId);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to set default address",
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
    }
}