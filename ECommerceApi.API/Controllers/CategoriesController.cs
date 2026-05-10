using ECommerceApi.Services.DTOs.Category;
using ECommerceApi.Services.DTOs.Common;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/categories")]
    public class CategoriesController(ICategoryService categoryService) : ControllerBase
    {
        // ── GET /api/v1/categories ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await categoryService.GetAllAsync();
            return Ok(ApiResponse<List<CategoryDto>>.Ok(categories));
        }

        // ── POST /api/v1/categories ──────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromForm] CreateCategoryDto dto)
        {
            var category = await categoryService.CreateAsync(dto);
            return Ok(ApiResponse<CategoryDto>.Ok(category, "تم إنشاء التصنيف"));
        }

        // ── PUT /api/v1/categories/{id} ──────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateCategoryDto dto)
        {
            var category = await categoryService.UpdateAsync(id, dto);
            if (category == null) return NotFound(ApiResponse<string>.Fail("التصنيف غير موجود"));
            return Ok(ApiResponse<CategoryDto>.Ok(category, "تم التحديث"));
        }

        // ── DELETE /api/v1/categories/{id} ───────────────────────
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await categoryService.DeleteAsync(id);
            if (!result) return NotFound(ApiResponse<string>.Fail("التصنيف غير موجود"));
            return Ok(ApiResponse<string>.Ok("تم حذف التصنيف"));
        }
    }
}
