using ECommerceApi.Services.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse<UserProfileDto>> GetProfileAsync(string userId);
        Task<ApiResponse<bool>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<ApiResponse<List<AddressDto>>> GetAddressesAsync(string userId);
        Task<ApiResponse<AddressDto>> AddAddressAsync(string userId, CreateAddressDto dto);
        Task<ApiResponse<bool>> UpdateAddressAsync(string userId, int addressId, CreateAddressDto dto);
        Task<ApiResponse<bool>> DeleteAddressAsync(string userId, int addressId);
        Task<ApiResponse<AddressDto>> SetDefaultAddressAsync(string userId, int addressId);
    }

}
