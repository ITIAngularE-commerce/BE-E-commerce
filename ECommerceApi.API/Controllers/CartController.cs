//using ECommerceApi.Services.DTOs.Cart;
//using ECommerceApi.Services.DTOs.Common;
//using ECommerceApi.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;

//namespace ECommerceApi.API.Controllers
//{
//    [ApiController]
//    [Route("api/v1/cart")]
//    [Authorize]
//    public class CartController(ICartService cartService) : ControllerBase
//    {
//        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

//        // ── GET /api/v1/cart ─────────────────────────────────────
//        [HttpGet]
//        public async Task<IActionResult> GetCart()
//        {
//            var cart = await cartService.GetCartAsync(UserId);
//            return Ok(ApiResponse<CartDto>.Ok(cart));
//        }

//        // ── POST /api/v1/cart/items ──────────────────────────────
//        [HttpPost("items")]
//        public async Task<IActionResult> AddItem(AddToCartDto dto)
//        {
//            try
//            {
//                var cart = await cartService.AddItemAsync(UserId, dto);
//                return Ok(ApiResponse<CartDto>.Ok(cart, "تم إضافة المنتج للكارت"));
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(ApiResponse<string>.Fail(ex.Message));
//            }
//        }

//        // ── PATCH /api/v1/cart/items/{id} ────────────────────────
//        [HttpPatch("items/{id}")]
//        public async Task<IActionResult> UpdateItem(int id, UpdateCartItemDto dto)
//        {
//            try
//            {
//                var cart = await cartService.UpdateItemAsync(UserId, id, dto);
//                return Ok(ApiResponse<CartDto>.Ok(cart, "تم التحديث"));
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(ApiResponse<string>.Fail(ex.Message));
//            }
//        }

//        // ── DELETE /api/v1/cart/items/{id} ───────────────────────
//        [HttpDelete("items/{id}")]
//        public async Task<IActionResult> RemoveItem(int id)
//        {
//            var cart = await cartService.RemoveItemAsync(UserId, id);
//            return Ok(ApiResponse<CartDto>.Ok(cart, "تم الحذف من الكارت"));
//        }

//        // ── DELETE /api/v1/cart/clear ────────────────────────────
//        [HttpDelete("clear")]
//        public async Task<IActionResult> ClearCart()
//        {
//            await cartService.ClearCartAsync(UserId);
//            return Ok(ApiResponse<string>.Ok("تم مسح الكارت"));
//        }
//    }


//}
