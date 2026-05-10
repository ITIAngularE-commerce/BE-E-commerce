//using ECommerceApi.Services.DTOs.Common;
//using ECommerceApi.Services.DTOs.Product;
//using ECommerceApi.Services.DTOs.Review;
//using ECommerceApi.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using System.Security.Claims;

//namespace ECommerceApi.API.Controllers
//{
//    [ApiController]
//    [Route("api/v1/products")]
//    public class ProductsController(IProductService productService,
//        IReviewService reviewService, IWishlistService wishlistService) : ControllerBase
//    {
//        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
//        private string Role => User.FindFirstValue(ClaimTypes.Role) ?? "Customer";

//        // ── GET /api/v1/products ─────────────────────────────────
//        [HttpGet]
//        public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
//        {
//            var result = await productService.GetAllAsync(filter);
//            return Ok(ApiResponse<PagedResultDto<ProductDto>>.Ok(result));
//        }

//        // ── GET /api/v1/products/{id} ────────────────────────────
//        [HttpGet("{id}")]
//        public async Task<IActionResult> GetById(int id)
//        {
//            var product = await productService.GetByIdAsync(id);
//            if (product == null) return NotFound(ApiResponse<string>.Fail("المنتج غير موجود"));
//            return Ok(ApiResponse<ProductDto>.Ok(product));
//        }

//        // ── POST /api/v1/products ────────────────────────────────
//        [HttpPost]
//        [Authorize(Roles = "Seller,Admin")]
//        public async Task<IActionResult> Create([FromForm] CreateProductDto dto)
//        {
//            var product = await productService.CreateAsync(UserId!, dto);
//            return CreatedAtAction(nameof(GetById), new { id = product.Id },
//                ApiResponse<ProductDto>.Ok(product, "تم إضافة المنتج بنجاح"));
//        }

//        // ── PUT /api/v1/products/{id} ────────────────────────────
//        [HttpPut("{id}")]
//        [Authorize(Roles = "Seller,Admin")]
//        public async Task<IActionResult> Update(int id, UpdateProductDto dto)
//        {
//            var product = await productService.UpdateAsync(id, UserId!, dto);
//            if (product == null) return NotFound(ApiResponse<string>.Fail("المنتج غير موجود أو ليس لديك صلاحية"));
//            return Ok(ApiResponse<ProductDto>.Ok(product, "تم التحديث بنجاح"));
//        }

//        // ── DELETE /api/v1/products/{id} ─────────────────────────
//        [HttpDelete("{id}")]
//        [Authorize(Roles = "Seller,Admin")]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var result = await productService.DeleteAsync(id, UserId!, Role);
//            if (!result) return NotFound(ApiResponse<string>.Fail("المنتج غير موجود أو ليس لديك صلاحية"));
//            return Ok(ApiResponse<string>.Ok("تم حذف المنتج"));
//        }

//        // ── GET /api/v1/products/{id}/reviews ───────────────────
//        [HttpGet("{id}/reviews")]
//        public async Task<IActionResult> GetReviews(int id)
//        {
//            var reviews = await reviewService.GetProductReviewsAsync(id);
//            return Ok(ApiResponse<List<ReviewDto>>.Ok(reviews));
//        }

//        // ── POST /api/v1/products/{id}/reviews ──────────────────
//        [HttpPost("{id}/reviews")]
//        [Authorize]
//        public async Task<IActionResult> AddReview(int id, CreateReviewDto dto)
//        {
//            try
//            {
//                var review = await reviewService.CreateAsync(id, UserId!, dto);
//                return CreatedAtAction(nameof(GetReviews), new { id },
//                    ApiResponse<ReviewDto>.Ok(review, "تم إضافة التقييم"));
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(ApiResponse<string>.Fail(ex.Message));
//            }
//        }

//        // ── POST /api/v1/products/{id}/wishlist ──────────────────
//        [HttpPost("{id}/wishlist")]
//        [Authorize]
//        public async Task<IActionResult> ToggleWishlist(int id)
//        {
//            var added = await wishlistService.ToggleAsync(UserId!, id);
//            var msg = added ? "تم الإضافة للمفضلة" : "تم الحذف من المفضلة";
//            return Ok(ApiResponse<bool>.Ok(added, msg));
//        }
//    }
//}
