using ECommerceApi.Services.DTOs.Auth;
using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController(IAuthService authService, IConfiguration _config) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await authService.RegisterAsync(dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Registration failed",
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
                message = "Registration successful",
                data = result.Data
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await authService.LoginAsync(dto);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Login failed",
                        errors = result.Errors
                    });
                }

                return Unauthorized(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = result.Data
            });
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto dto)
        {
            var result = await authService.GoogleLoginAsync(dto.IdToken);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Google login failed",
                        errors = result.Errors
                    });
                }

                return Unauthorized(new
                {
                    success = false,
                    message = result.ErrorMessage
                });
            }

            return Ok(new
            {
                success = true,
                message = "Login successful",
                data = result.Data
            });
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId))
            {
                var fullQuery = Request.QueryString.ToString();

                var userIdMatch = System.Text.RegularExpressions.Regex.Match(fullQuery, @"userId[=:]?([a-fA-F0-9-]+)");
                if (userIdMatch.Success)
                {
                    userId = userIdMatch.Groups[1].Value;
                }
            }

            if (string.IsNullOrEmpty(token))
            {
                var fullQuery = Request.QueryString.ToString();
                var tokenMatch = System.Text.RegularExpressions.Regex.Match(fullQuery, @"token[=:]?([^&]+)");
                if (tokenMatch.Success)
                {
                    token = Uri.UnescapeDataString(tokenMatch.Groups[1].Value);
                }
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid confirmation link. Missing userId or token."
                });
            }

            var result = await authService.ConfirmEmailAsync(userId, token);

            if (!result.IsSuccess)
            {
                if (result.Errors != null && result.Errors.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email confirmation failed",
                        errors = result.Errors
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.ErrorMessage ?? "Email confirmation failed"
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message ?? "Email confirmed successfully",
                data = new
                {
                    emailConfirmed = true,
                    redirectUrl = $"{_config["FrontendUrl"]}/login"
                }
            });
        }
    }
}