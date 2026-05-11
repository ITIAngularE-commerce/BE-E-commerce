using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.DTOs.Product
{
    public class ProductFilterDto
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } = "createdAt"; // price, rating
        public bool Ascending { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
