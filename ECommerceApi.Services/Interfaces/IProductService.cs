using ECommerceApi.Services.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductDto>> GetAllAsync(ProductFilterDto filter);
        Task<ProductDto?> GetByIdAsync(int id);
        Task<ProductDto> CreateAsync(string sellerId, CreateProductDto dto);
        Task<ProductDto?> UpdateAsync(int id, string sellerId, UpdateProductDto dto);
        Task<bool> DeleteAsync(int id, string sellerId, string role);
    }
}
