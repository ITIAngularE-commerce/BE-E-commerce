
namespace ECommerceApi.Services.Implementations
{
    public class PaymentService(
        AppDbContext db,
        IPaymobService paymob,
        UserManager<ApplicationUser> userManager) : IPaymentService
    {
        public async Task<ApiResponse<InitiatePaymentResponse>> InitiatePaymobAsync(
            int orderId, string userId)
        {
            try
            {
                var order = await db.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order is null)
                    return ApiResponse<InitiatePaymentResponse>.Failure("Order not found");

                var existingPayment = await db.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId
                                           && p.Status == PaymentStatus.Completed);

                if (existingPayment is not null)
                    return ApiResponse<InitiatePaymentResponse>.Failure("Order already paid");

                var nameParts = (order.User?.FullName ?? "Guest User").Split(' ');
                var billing = new BillingInfo
                {
                    FirstName = nameParts[0],
                    LastName = nameParts.Length > 1 ? nameParts[1] : "N/A",
                    Email = order.User?.Email ?? "guest@example.com",
                    PhoneNumber = order.User?.PhoneNumber ?? "01000000000"
                };

                var payment = await db.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);

                if (payment is null)
                {
                    payment = new Payment
                    {
                        OrderId = orderId,
                        Provider = "Paymob",
                        Amount = order.Total,
                        Status = PaymentStatus.Pending
                    };
                    db.Payments.Add(payment);
                    await db.SaveChangesAsync();
                }

                var result = await paymob.InitiatePaymentAsync(orderId, order.Total, billing);

                return ApiResponse<InitiatePaymentResponse>.Success(result,
                    "Payment initiated. Complete payment in the iframe.");
            }
            catch (Exception ex)
            {
                return ApiResponse<InitiatePaymentResponse>.Failure($"Failed to initiate payment: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> HandleCallbackAsync(
            PaymobCallbackDto callback)
        {
            try
            {
                if (callback.Obj is null)
                    return ApiResponse<bool>.Failure("Invalid callback data");

                var merchantOrderId = callback.Obj.Order?.MerchantOrderId
                                   ?? callback.Obj.MerchantOrderId;

                if (!int.TryParse(merchantOrderId, out var orderId))
                    return ApiResponse<bool>.Failure("Invalid order reference");

                var payment = await db.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId
                                           && p.Provider == "Paymob");

                if (payment is null)
                    return ApiResponse<bool>.Failure("Payment record not found");

                payment.TransactionId = callback.Obj.Id.ToString();
                payment.Status = callback.Obj.Success
                    ? PaymentStatus.Completed
                    : PaymentStatus.Failed;
                payment.PaidAt = callback.Obj.Success ? DateTime.UtcNow : null;

                if (callback.Obj.Success)
                {
                    var order = await db.Orders.FindAsync(orderId);
                    if (order is not null && order.Status == OrderStatus.Pending)
                        order.Status = OrderStatus.Processing;
                }

                await db.SaveChangesAsync();
                return ApiResponse<bool>.Success(true, "Payment status updated");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"Failed to process callback: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaymentDto>> GetPaymentStatusAsync(int orderId, string userId)
        {
            try
            {
                var payment = await db.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Order.UserId == userId);

                if (payment is null)
                    return ApiResponse<PaymentDto>.Failure("Payment not found");

                var paymentDto = new PaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Provider = payment.Provider,
                    TransactionId = payment.TransactionId,
                    Amount = payment.Amount,
                    Status = payment.Status.ToString(),
                    PaidAt = payment.PaidAt
                };

                return ApiResponse<PaymentDto>.Success(paymentDto, "Payment status retrieved");
            }
            catch (Exception ex)
            {
                return ApiResponse<PaymentDto>.Failure($"Failed to get payment status: {ex.Message}");
            }
        }
    }
}