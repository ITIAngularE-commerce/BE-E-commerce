using System.IdentityModel.Tokens.Jwt;
using ECommerceApi.Data.Models;
using ECommerceApi.Services.DTOs.Auth;
using ECommerceApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;

namespace ECommerceApi.Services.Implementations
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        IEmailService emailService) : IAuthService
    {
        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
        {
            var existing = await userManager.FindByEmailAsync(dto.Email);
            if (existing != null) return null;

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return null;

            // إضافة الـ Role
            var role = dto.Role is "Seller" or "Admin" ? dto.Role : "Customer";
            await userManager.AddToRoleAsync(user, role);

            // إنشاء كارت فارغ للمستخدم — هيتعمل في CartService أول ما يطلبه

            // إرسال تأكيد الإيميل
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = $"https://localhost:7001/api/v1/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            await emailService.SendEmailConfirmationAsync(user.Email!, user.FullName, link);

            return await GenerateTokenAsync(user);
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null || !user.IsActive) return null;

            var valid = await userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid) return null;

            return await GenerateTokenAsync(user);
        }

        public async Task<AuthResponseDto?> GoogleLoginAsync(string idToken)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [config["Google:ClientId"]!]
                });
            }
            catch { return null; }

            var user = await userManager.FindByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    FullName = payload.Name,
                    Email = payload.Email,
                    UserName = payload.Email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user);
                await userManager.AddToRoleAsync(user, "Customer");
            }

            if (!user.IsActive) return null;
            return await GenerateTokenAsync(user);
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public Task<bool> LogoutAsync(string userId)
        {
            // JWT stateless — الـ logout بيتعمل من الـ client بحذف التوكن
            // لو محتاجين blacklist نضيف Redis هنا
            return Task.FromResult(true);
        }

        // ─── Private Helpers ────────────────────────────────────────

        private async Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";
            var expiry = DateTime.UtcNow.AddDays(double.Parse(config["JWT:DurationInDays"]!));

            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email,          user.Email!),
            new(ClaimTypes.Name,           user.FullName),
            new(ClaimTypes.Role,           role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["JWT:Issuer"],
                audience: config["JWT:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = GenerateRefreshToken(),
                Expiry = expiry,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                Role = role
            };
        }

        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
 }
