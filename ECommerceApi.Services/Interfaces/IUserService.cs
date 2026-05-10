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
        Task<UserProfileDto?> GetProfileAsync(string userId);
        Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<List<AddressDto>> GetAddressesAsync(string userId);
        Task<AddressDto> AddAddressAsync(string userId, CreateAddressDto dto);
        Task<bool> DeleteAddressAsync(string userId, int addressId);
    }

}
