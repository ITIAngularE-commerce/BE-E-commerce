using Microsoft.AspNetCore.Http;

namespace ECommerceApi.Services.DTOs.Product
{
    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public List<IFormFile> Images { get; set; } = [];
    }
}
