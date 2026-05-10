
namespace ECommerceApi.Services.Interfaces
{
        public interface IAuthService
        {
            Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto);
            Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
            Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string idToken);
            Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token);
            Task<bool> LogoutAsync(string userId);
        }
}
