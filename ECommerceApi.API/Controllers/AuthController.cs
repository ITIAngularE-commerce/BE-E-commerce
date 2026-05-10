using ECommerceApi.Services.DTOs.Auth;
using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await authService.RegisterAsync(dto);
            if (result == null)
                return BadRequest(ApiResponse<string>.Fail("البريد الإلكتروني مستخدم بالفعل"));

            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "تم التسجيل بنجاح"));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await authService.LoginAsync(dto);
            if (result == null)
                return Unauthorized(ApiResponse<string>.Fail("بيانات الدخول غير صحيحة"));

            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "تم تسجيل الدخول بنجاح"));
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleAuthDto dto)
        {
            var result = await authService.GoogleLoginAsync(dto.IdToken);
            if (result == null)
                return Unauthorized(ApiResponse<string>.Fail("Google token غير صالح"));

            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "تم تسجيل الدخول بنجاح"));
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var result = await authService.ConfirmEmailAsync(userId, token);
            if (!result)
                return BadRequest(ApiResponse<string>.Fail("رابط التأكيد غير صالح أو منتهي"));

            return Ok(ApiResponse<string>.Ok("تم تأكيد البريد الإلكتروني بنجاح"));
        }
    }
}
