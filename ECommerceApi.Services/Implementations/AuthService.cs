
namespace ECommerceApi.Services.Implementations
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        IEmailService emailService) : IAuthService
    {
        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            var existing = await userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return ApiResponse<AuthResponseDto>.Failure("Email is already registered");

            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<AuthResponseDto>.Failure(errors);
            }

            var role = dto.Role is "Seller" or "Admin" ? dto.Role : "Customer";
            await userManager.AddToRoleAsync(user, role);

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = $"{config["AppUrl"]}/api/v1/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            try
            {
                await emailService.SendEmailConfirmationAsync(user.Email!, user.FullName, confirmationLink);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send confirmation email to {user.Email}: {ex.Message}");
            }

            var authResponse = await GenerateTokenAsync(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "Registration successful. Please check your email to confirm your account.");
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return ApiResponse<AuthResponseDto>.Failure("No account found with this email");

            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Failure("This account is inactive. Please contact support");

            var valid = await userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                return ApiResponse<AuthResponseDto>.Failure("Incorrect password");

            var authResponse = await GenerateTokenAsync(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "Login successful");
        }

        public async Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string idToken)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [config["Google:ClientId"]!]
                });
            }
            catch
            {
                return ApiResponse<AuthResponseDto>.Failure("Invalid Google token");
            }

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

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    return ApiResponse<AuthResponseDto>.Failure(errors);
                }

                await userManager.AddToRoleAsync(user, "Customer");
            }

            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Failure("This account is inactive");

            var authResponse = await GenerateTokenAsync(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse, "Login successful");
        }

        public async Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.Failure("User not found");

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.Failure(errors);
            }

            try
            {
                await emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send welcome email: {ex.Message}");
            }

            return ApiResponse<string>.Success("Your email has been confirmed successfully!", "Email confirmed successfully");
        }

        public Task<bool> LogoutAsync(string userId)
        {
            return Task.FromResult(true);
        }

        private async Task<AuthResponseDto> GenerateTokenAsync(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Customer";
            var expiry = DateTime.UtcNow.AddDays(double.Parse(config["JWT:DurationInDays"]!));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, role),
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