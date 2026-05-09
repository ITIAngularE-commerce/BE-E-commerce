using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.DTOs.Order
{
    public class CreateOrderDto
    {
        public int AddressId { get; set; }
        public string PaymentMethod { get; set; } = "CashOnDelivery";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TrackingCode { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public AddressSnapshotDto Address { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = [];
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class AddressSnapshotDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
