
namespace ECommerceApi.Services.Implementations
{
    public class OrderService(AppDbContext db, IEmailService emailService,
        UserManager<ApplicationUser> userManager) : IOrderService
    {
        public async Task<ApiResponse<OrderDto>> CreateAsync(string userId, CreateOrderDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<OrderDto>.Failure("User ID is required");

                var cart = await db.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.Items.Any())
                    return ApiResponse<OrderDto>.Failure("Cart is empty");

                if (dto.AddressId <= 0)
                    return ApiResponse<OrderDto>.Failure("Valid address ID is required");

                var address = await db.Addresses.FindAsync(dto.AddressId);
                if (address == null)
                    return ApiResponse<OrderDto>.Failure("Address not found");

                if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
                    return ApiResponse<OrderDto>.Failure("Payment method is required");

                if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var paymentMethod))
                    return ApiResponse<OrderDto>.Failure($"Invalid payment method. Valid values: {string.Join(", ", Enum.GetNames<PaymentMethod>())}");

                foreach (var item in cart.Items)
                {
                    if (item.Product == null)
                        return ApiResponse<OrderDto>.Failure($"Product with ID {item.ProductId} not found");

                    if (item.Product.Stock < item.Quantity)
                        return ApiResponse<OrderDto>.Failure($"Insufficient stock for product '{item.Product.Name}'. Available: {item.Product.Stock}, Requested: {item.Quantity}");
                }

                var order = new Order
                {
                    UserId = userId,
                    AddressId = dto.AddressId,
                    PaymentMethod = paymentMethod,
                    TrackingCode = GenerateTrackingCode(),
                    Status = OrderStatus.Pending
                };

                foreach (var item in cart.Items)
                {
                    order.Items.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product!.Price
                    });
                    item.Product.Stock -= item.Quantity;
                }

                order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

                db.Orders.Add(order);
                db.CartItems.RemoveRange(cart.Items);
                await db.SaveChangesAsync();

                var user = await userManager.FindByIdAsync(userId);
                if (user?.Email != null)
                {
                    try
                    {
                        await emailService.SendOrderConfirmationAsync(user.Email, user.FullName, order.Id, order.Total);
                    }
                    catch
                    {
                        // Email failed but order still created
                    }
                }

                var createdOrder = await GetByIdAsync(order.Id, userId, "Customer");
                if (paymentMethod == PaymentMethod.CreditCard)
                {
                    db.Payments.Add(new Payment
                    {
                        OrderId = order.Id,
                        Provider = "COD",
                        Amount = order.Total,
                        Status = PaymentStatus.Pending
                    });
                    await db.SaveChangesAsync();
                }
                return ApiResponse<OrderDto>.Success(createdOrder.Data!, "Order created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<OrderDto>.Failure($"An error occurred while creating order: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<OrderDto>>> GetUserOrdersAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<List<OrderDto>>.Failure("User ID is required");

                var orders = await db.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.Address)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                if (!orders.Any())
                    return ApiResponse<List<OrderDto>>.Success(new List<OrderDto>(), "No orders found");

                var orderDtos = orders.Select(MapToDto).ToList();
                return ApiResponse<List<OrderDto>>.Success(orderDtos, $"Successfully retrieved {orderDtos.Count} order(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OrderDto>>.Failure($"An error occurred while retrieving orders: {ex.Message}");
            }
        }

        public async Task<ApiResponse<OrderDto>> GetByIdAsync(int id, string userId, string role)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<OrderDto>.Failure("Invalid order ID");

                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<OrderDto>.Failure("User ID is required");

                if (string.IsNullOrWhiteSpace(role))
                    role = "Customer";

                var order = await db.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.Address)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return ApiResponse<OrderDto>.Failure($"Order with ID {id} not found");

                if (role == "Customer" && order.UserId != userId)
                    return ApiResponse<OrderDto>.Failure("You don't have permission to view this order");

                return ApiResponse<OrderDto>.Success(MapToDto(order), "Order retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<OrderDto>.Failure($"An error occurred while retrieving order: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CancelAsync(int id, string userId)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<bool>.Failure("Invalid order ID");

                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                var order = await db.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

                if (order == null)
                    return ApiResponse<bool>.Failure($"Order with ID {id} not found");

                if (order.Status != OrderStatus.Pending)
                    return ApiResponse<bool>.Failure($"Cannot cancel order with status '{order.Status}'. Only pending orders can be cancelled");

                order.Status = OrderStatus.Cancelled;

                foreach (var item in order.Items)
                {
                    if (item.Product != null)
                        item.Product.Stock += item.Quantity;
                }

                await db.SaveChangesAsync();
                return ApiResponse<bool>.Success(true, "Order cancelled successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while cancelling order: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateStatusAsync(int id, string status, string adminId)
        {
            try
            {
                if (id <= 0)
                    return ApiResponse<bool>.Failure("Invalid order ID");

                if (string.IsNullOrWhiteSpace(status))
                    return ApiResponse<bool>.Failure("Status is required");

                if (string.IsNullOrWhiteSpace(adminId))
                    return ApiResponse<bool>.Failure("Admin ID is required");

                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                    return ApiResponse<bool>.Failure($"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}");

                var order = await db.Orders.FindAsync(id);
                if (order == null)
                    return ApiResponse<bool>.Failure($"Order with ID {id} not found");

                if (order.Status == OrderStatus.Cancelled)
                    return ApiResponse<bool>.Failure("Cannot update status of a cancelled order");

                if (order.Status == OrderStatus.Delivered)
                    return ApiResponse<bool>.Failure("Cannot update status of a delivered order");

                order.Status = orderStatus;
                await db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, $"Order status updated to {orderStatus}");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while updating order status: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<OrderDto>>> GetAllOrdersAsync(string adminId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(adminId))
                    return ApiResponse<List<OrderDto>>.Failure("Admin ID is required");

                var orders = await db.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.Address)
                    .Include(o => o.User)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                if (!orders.Any())
                    return ApiResponse<List<OrderDto>>.Success(new List<OrderDto>(), "No orders found");

                var orderDtos = orders.Select(MapToDto).ToList();
                return ApiResponse<List<OrderDto>>.Success(orderDtos, $"Successfully retrieved {orderDtos.Count} order(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OrderDto>>.Failure($"An error occurred while retrieving all orders: {ex.Message}");
            }
        }

        private static string GenerateTrackingCode()
        {
            return Guid.NewGuid().ToString("N")[..10].ToUpper();
        }

        private static OrderDto MapToDto(Order o)
        {
            return new OrderDto
            {
                Id = o.Id,
                Status = o.Status.ToString(),
                PaymentMethod = o.PaymentMethod.ToString(),
                TrackingCode = o.TrackingCode,
                Total = o.Total,
                CreatedAt = o.CreatedAt,
                Address = new AddressSnapshotDto
                {
                    Street = o.Address?.Street ?? string.Empty,
                    City = o.Address?.City ?? string.Empty,
                    Country = o.Address?.Country ?? string.Empty
                },
                Items = o.Items.Select(i =>
                {
                    var images = string.IsNullOrEmpty(i.Product?.ImageUrls)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(i.Product.ImageUrls) ?? new List<string>();

                    return new OrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product?.Name ?? string.Empty,
                        ImageUrl = images.FirstOrDefault(),
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Subtotal = i.UnitPrice * i.Quantity
                    };
                }).ToList()
            };
        }
    }
}