using ECommerceApi.Services.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerceApi.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<AuthResponseDto?> GoogleLoginAsync(string idToken);
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task<bool> LogoutAsync(string userId);
    }
}
