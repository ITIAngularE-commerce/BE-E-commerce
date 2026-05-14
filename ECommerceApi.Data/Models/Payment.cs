
namespace ECommerceApi.Data.Models
{

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Provider { get; set; } = string.Empty;   
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime? PaidAt { get; set; }
        public Order Order { get; set; } = null!;
    }
}
