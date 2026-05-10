//using ECommerceApi.Services.DTOs.Common;
//using ECommerceApi.Services.DTOs.Product;
//using ECommerceApi.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;

//namespace ECommerceApi.API.Controllers
//{
//    [ApiController]
//    [Route("api/v1/wishlist")]
//    [Authorize]
//    public class WishlistController(IWishlistService wishlistService) : ControllerBase
//    {
//        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

//        // ── GET /api/v1/wishlist ─────────────────────────────────
//        [HttpGet]
//        public async Task<IActionResult> GetWishlist()
//        {
//            var items = await wishlistService.GetWishlistAsync(UserId);
//            return Ok(ApiResponse<List<ProductDto>>.Ok(items));
//        }

//        // ── POST /api/v1/wishlist/{productId} ────────────────────
//        [HttpPost("{productId}")]
//        public async Task<IActionResult> Toggle(int productId)
//        {
//            var added = await wishlistService.ToggleAsync(UserId, productId);
//            var msg = added ? "تم الإضافة للمفضلة" : "تم الحذف من المفضلة";
//            return Ok(ApiResponse<bool>.Ok(added, msg));
//        }
//    }
//}
