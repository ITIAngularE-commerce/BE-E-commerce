using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.DTOs.User;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    public class UsersController(IUserService userService) : ControllerBase
    {
        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var profile = await userService.GetProfileAsync(UserId);
            if (profile == null) return NotFound(ApiResponse<string>.Fail("المستخدم غير موجود"));
            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var result = await userService.UpdateProfileAsync(UserId, dto);
            if (!result) return BadRequest(ApiResponse<string>.Fail("فشل التحديث"));
            return Ok(ApiResponse<string>.Ok("تم التحديث بنجاح"));
        }

        [HttpGet("me/addresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var addresses = await userService.GetAddressesAsync(UserId);
            return Ok(ApiResponse<List<AddressDto>>.Ok(addresses));
        }

        [HttpPost("me/addresses")]
        public async Task<IActionResult> AddAddress(CreateAddressDto dto)
        {
            var address = await userService.AddAddressAsync(UserId, dto);
            return CreatedAtAction(nameof(GetAddresses), ApiResponse<AddressDto>.Ok(address, "تم إضافة العنوان"));
        }

        [HttpDelete("me/addresses/{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var result = await userService.DeleteAddressAsync(UserId, id);
            if (!result) return NotFound(ApiResponse<string>.Fail("العنوان غير موجود"));
            return Ok(ApiResponse<string>.Ok("تم حذف العنوان"));
        }
    }
}
