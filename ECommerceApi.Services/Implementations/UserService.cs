using ECommerceApi.Data.Models;
using ECommerceApi.Data;
using ECommerceApi.Services.DTOs.User;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace ECommerceApi.Services.Implementations
{
    public class UserService(UserManager<ApplicationUser> userManager, AppDbContext db) : IUserService
    {
        public async Task<UserProfileDto?> GetProfileAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return null;
            var roles = await userManager.GetRolesAsync(user);

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber ?? "",
                Role = roles.FirstOrDefault() ?? "Customer",
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            var result = await userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<List<AddressDto>> GetAddressesAsync(string userId)
        {
            return await db.Addresses
                .Where(a => a.UserId == userId)
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    Street = a.Street,
                    City = a.City,
                    Country = a.Country,
                    IsDefault = a.IsDefault
                }).ToListAsync();
        }

        public async Task<AddressDto> AddAddressAsync(string userId, CreateAddressDto dto)
        {
            if (dto.IsDefault)
            {
                var existing = await db.Addresses.Where(a => a.UserId == userId).ToListAsync();
                existing.ForEach(a => a.IsDefault = false);
            }

            var address = new Address
            {
                UserId = userId,
                Street = dto.Street,
                City = dto.City,
                Country = dto.Country,
                IsDefault = dto.IsDefault
            };

            db.Addresses.Add(address);
            await db.SaveChangesAsync();

            return new AddressDto
            {
                Id = address.Id,
                Street = address.Street,
                City = address.City,
                Country = address.Country,
                IsDefault = address.IsDefault
            };
        }

        public async Task<bool> DeleteAddressAsync(string userId, int addressId)
        {
            var address = await db.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
            if (address == null) return false;

            db.Addresses.Remove(address);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
