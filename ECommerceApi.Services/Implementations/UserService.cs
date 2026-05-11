
namespace ECommerceApi.Services.Implementations
{
    public class UserService(UserManager<ApplicationUser> userManager, AppDbContext db) : IUserService
    {
        public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<UserProfileDto>.Failure("User ID is required");

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return ApiResponse<UserProfileDto>.Failure("User not found");

                var roles = await userManager.GetRolesAsync(user);

                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Customer",
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive
                };

                return ApiResponse<UserProfileDto>.Success(profile, "Profile retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<UserProfileDto>.Failure($"An error occurred while retrieving profile: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                if (string.IsNullOrWhiteSpace(dto.FullName))
                    return ApiResponse<bool>.Failure("Full name is required");

                if (dto.FullName.Length < 3)
                    return ApiResponse<bool>.Failure("Full name must be at least 3 characters");

                if (dto.FullName.Length > 100)
                    return ApiResponse<bool>.Failure("Full name cannot exceed 100 characters");

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                    return ApiResponse<bool>.Failure("User not found");

                user.FullName = dto.FullName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    user.PhoneNumber = dto.PhoneNumber;
                }

                var result = await userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<bool>.Failure(errors);
                }

                return ApiResponse<bool>.Success(true, "Profile updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while updating profile: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<AddressDto>>> GetAddressesAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<List<AddressDto>>.Failure("User ID is required");

                var addresses = await db.Addresses
                    .Where(a => a.UserId == userId)
                    .Select(a => new AddressDto
                    {
                        Id = a.Id,
                        Street = a.Street,
                        City = a.City,
                        Country = a.Country,
                        IsDefault = a.IsDefault
                    })
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.Id)
                    .ToListAsync();

                if (!addresses.Any())
                    return ApiResponse<List<AddressDto>>.Success(new List<AddressDto>(), "No addresses found");

                return ApiResponse<List<AddressDto>>.Success(addresses, $"Successfully retrieved {addresses.Count} address(es)");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<AddressDto>>.Failure($"An error occurred while retrieving addresses: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AddressDto>> AddAddressAsync(string userId, CreateAddressDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<AddressDto>.Failure("User ID is required");

                if (string.IsNullOrWhiteSpace(dto.Street))
                    return ApiResponse<AddressDto>.Failure("Street address is required");

                if (string.IsNullOrWhiteSpace(dto.City))
                    return ApiResponse<AddressDto>.Failure("City is required");

                if (string.IsNullOrWhiteSpace(dto.Country))
                    return ApiResponse<AddressDto>.Failure("Country is required");

                if (dto.Street.Length < 5)
                    return ApiResponse<AddressDto>.Failure("Street address must be at least 5 characters");

                if (dto.City.Length < 2)
                    return ApiResponse<AddressDto>.Failure("City name must be at least 2 characters");

                var userExists = await userManager.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                    return ApiResponse<AddressDto>.Failure("User not found");

                if (dto.IsDefault)
                {
                    var existingAddresses = await db.Addresses.Where(a => a.UserId == userId).ToListAsync();
                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                var address = new Address
                {
                    UserId = userId,
                    Street = dto.Street.Trim(),
                    City = dto.City.Trim(),
                    Country = dto.Country.Trim(),
                    IsDefault = dto.IsDefault
                };

                db.Addresses.Add(address);
                await db.SaveChangesAsync();

                // If this was the first address, make it default
                if (!dto.IsDefault)
                {
                    var addressCount = await db.Addresses.CountAsync(a => a.UserId == userId);
                    if (addressCount == 1)
                    {
                        address.IsDefault = true;
                        await db.SaveChangesAsync();
                    }
                }

                var addressDto = new AddressDto
                {
                    Id = address.Id,
                    Street = address.Street,
                    City = address.City,
                    Country = address.Country,
                    IsDefault = address.IsDefault
                };

                return ApiResponse<AddressDto>.Success(addressDto, "Address added successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<AddressDto>.Failure($"An error occurred while adding address: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateAddressAsync(string userId, int addressId, CreateAddressDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                if (addressId <= 0)
                    return ApiResponse<bool>.Failure("Invalid address ID");

                var address = await db.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

                if (address == null)
                    return ApiResponse<bool>.Failure("Address not found");

                if (!string.IsNullOrWhiteSpace(dto.Street))
                {
                    if (dto.Street.Length < 5)
                        return ApiResponse<bool>.Failure("Street address must be at least 5 characters");
                    address.Street = dto.Street.Trim();
                }

                if (!string.IsNullOrWhiteSpace(dto.City))
                {
                    if (dto.City.Length < 2)
                        return ApiResponse<bool>.Failure("City name must be at least 2 characters");
                    address.City = dto.City.Trim();
                }

                if (!string.IsNullOrWhiteSpace(dto.Country))
                    address.Country = dto.Country.Trim();

                await db.SaveChangesAsync();

                return ApiResponse<bool>.Success(true, "Address updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while updating address: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteAddressAsync(string userId, int addressId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<bool>.Failure("User ID is required");

                if (addressId <= 0)
                    return ApiResponse<bool>.Failure("Invalid address ID");

                var address = await db.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

                if (address == null)
                    return ApiResponse<bool>.Failure("Address not found");

                var wasDefault = address.IsDefault;

                db.Addresses.Remove(address);
                await db.SaveChangesAsync();

                // If the deleted address was default, set another address as default
                if (wasDefault)
                {
                    var newDefault = await db.Addresses
                        .Where(a => a.UserId == userId)
                        .FirstOrDefaultAsync();

                    if (newDefault != null)
                    {
                        newDefault.IsDefault = true;
                        await db.SaveChangesAsync();
                    }
                }

                return ApiResponse<bool>.Success(true, "Address deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"An error occurred while deleting address: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AddressDto>> SetDefaultAddressAsync(string userId, int addressId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiResponse<AddressDto>.Failure("User ID is required");

                if (addressId <= 0)
                    return ApiResponse<AddressDto>.Failure("Invalid address ID");

                var address = await db.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

                if (address == null)
                    return ApiResponse<AddressDto>.Failure("Address not found");

                var existingAddresses = await db.Addresses.Where(a => a.UserId == userId).ToListAsync();
                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }

                address.IsDefault = true;
                await db.SaveChangesAsync();

                var addressDto = new AddressDto
                {
                    Id = address.Id,
                    Street = address.Street,
                    City = address.City,
                    Country = address.Country,
                    IsDefault = address.IsDefault
                };

                return ApiResponse<AddressDto>.Success(addressDto, "Default address updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<AddressDto>.Failure($"An error occurred while setting default address: {ex.Message}");
            }
        }
    }
}