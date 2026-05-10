
namespace ECommerceApi.Services.Implementations
{
    public class AdminService(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IOrderService orderService) : IAdminService
    {
        public async Task<ApiResponse<List<UserProfileDto>>> GetAllUsersAsync(string? role)
        {
            try
            {
                // Validation: Check if role parameter is valid when provided
                if (!string.IsNullOrEmpty(role))
                {
                    var validRoles = new[] { "Admin", "Seller", "Customer" };
                    if (!validRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                    {
                        return ApiResponse<List<UserProfileDto>>.Failure(
                            $"Invalid role '{role}'. Valid roles are: {string.Join(", ", validRoles)}");
                    }
                }

                // Get users based on role filter
                List<ApplicationUser> users;

                if (!string.IsNullOrEmpty(role))
                {
                    users = (await userManager.GetUsersInRoleAsync(role)).ToList();
                }
                else
                {
                    users = await userManager.Users.ToListAsync();
                }

                // Validation: Check if any users found
                if (!users.Any())
                {
                    return ApiResponse<List<UserProfileDto>>.Success(
                        new List<UserProfileDto>(),
                        "No users found");
                }

                // Map users to DTOs
                var result = new List<UserProfileDto>();
                foreach (var u in users)
                {
                    var roles = await userManager.GetRolesAsync(u);
                    result.Add(new UserProfileDto
                    {
                        Id = u.Id,
                        FullName = u.FullName,
                        Email = u.Email!,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        Role = roles.FirstOrDefault() ?? "Customer",
                        CreatedAt = u.CreatedAt,
                        IsActive = u.IsActive
                    });
                }

                return ApiResponse<List<UserProfileDto>>.Success(
                    result,
                    $"Successfully retrieved {result.Count} user(s)");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<UserProfileDto>>.Failure(
                    $"An error occurred while retrieving users: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ToggleUserStatusAsync(string userId)
        {
            try
            {
                // Validation 1: Check if userId is provided
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiResponse<bool>.Failure("User ID is required");
                }

                // Validation 2: Check if userId is valid format (GUID)
                if (!Guid.TryParse(userId, out _))
                {
                    return ApiResponse<bool>.Failure("Invalid user ID format");
                }

                // Validation 3: Check if user exists
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<bool>.Failure("User not found");
                }

                // Validation 4: Prevent deactivating your own account
                // Note: You'll need to pass current userId from controller
                // For now, we'll add a parameter or handle in controller

                // Validation 5: Check if user is already in target state
                // This is optional but nice to have
                var newStatus = !user.IsActive;

                // Toggle user status
                user.IsActive = newStatus;
                var result = await userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<bool>.Failure(errors);
                }

                var statusMessage = newStatus ? "activated" : "deactivated";
                return ApiResponse<bool>.Success(
                    true,
                    $"User has been {statusMessage} successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure(
                    $"An error occurred while toggling user status: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<OrderDto>>> GetAllOrdersAsync()
        {
            try
            {
                // Validation: Check if database context is available
                if (db == null)
                {
                    return ApiResponse<List<OrderDto>>.Failure("Database context is not available");
                }

                // Get all orders with their related data
                var orders = await db.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.Address)
                    .Include(o => o.User)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                // Validation: Check if any orders exist
                if (!orders.Any())
                {
                    return ApiResponse<List<OrderDto>>.Success(
                        new List<OrderDto>(),
                        "No orders found");
                }

                // Map orders to DTOs
                var orderDtos = new List<OrderDto>();
                foreach (var order in orders)
                {
                    var orderDto = await orderService.GetByIdAsync(order.Id, "", "Admin");
                    if (orderDto != null)
                    {
                        orderDtos.Add(orderDto);
                    }
                }

                // Validation: Check if any orders were successfully mapped
                if (!orderDtos.Any())
                {
                    return ApiResponse<List<OrderDto>>.Failure(
                        "Failed to retrieve order details");
                }

                return ApiResponse<List<OrderDto>>.Success(
                    orderDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<OrderDto>>.Failure(
                    $"An error occurred while retrieving orders: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AdminStatsDto>> GetStatsAsync()
        {
            try
            {
                if (db == null)
                {
                    return ApiResponse<AdminStatsDto>.Failure("Database context is not available");
                }

                var totalUsers = await userManager.Users.CountAsync();
                var totalOrders = await db.Orders.CountAsync();
                var totalProducts = await db.Products.CountAsync(p => p.IsActive);
                var totalRevenue = await db.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.Total);

                var stats = new AdminStatsDto
                {
                    TotalUsers = totalUsers,
                    TotalOrders = totalOrders,
                    TotalProducts = totalProducts,
                    TotalRevenue = totalRevenue >= 0 ? totalRevenue : 0
                };

                var message = "Statistics retrieved successfully";
                if (stats.TotalUsers == 0 && stats.TotalOrders == 0 && stats.TotalProducts == 0)
                {
                    message = "Statistics retrieved successfully (all counts are zero)";
                }

                return ApiResponse<AdminStatsDto>.Success(stats, message);
            }
            catch (Exception ex)
            {
                return ApiResponse<AdminStatsDto>.Failure(
                    $"An error occurred while retrieving statistics: {ex.Message}");
            }
        }
    }
}