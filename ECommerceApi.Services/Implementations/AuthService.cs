
namespace ECommerceApi.Services.Implementations
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        IEmailService emailService) : IAuthService
    {
        public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto)
        {
            // 1. Check if email already exists
            var existing = await userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return ApiResponse<AuthResponseDto>.Failure("البريد الإلكتروني مسجل بالفعل");

            // 2. Create new user
            var user = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber
            };

            var result = await userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<AuthResponseDto>.Failure(errors);
            }

            // 3. Add role
            var role = dto.Role is "Seller" or "Admin" ? dto.Role : "Customer";
            await userManager.AddToRoleAsync(user, role);

            // 4. Send email confirmation (optional - don't fail registration if email fails)
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = $"https://localhost:7001/api/v1/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
            try
            {
                await emailService.SendEmailConfirmationAsync(user.Email!, user.FullName, link);
            }
            catch
            {
                // Email failed but registration still succeeds
                Console.WriteLine($"Failed to send email to {user.Email}");
            }

            // 5. Generate token and return success
            var authResponse = await GenerateTokenAsync(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse);
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            // 1. Find user by email
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return ApiResponse<AuthResponseDto>.Failure("لا يوجد حساب بهذا البريد الإلكتروني");

            // 2. Check if account is active
            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Failure("هذا الحساب غير نشط. يرجى التواصل مع الدعم");

            // 3. Check password
            var valid = await userManager.CheckPasswordAsync(user, dto.Password);
            if (!valid)
                return ApiResponse<AuthResponseDto>.Failure("كلمة المرور غير صحيحة");

            // 4. Generate token and return success
            var authResponse = await GenerateTokenAsync(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse);
        }

        public async Task<ApiResponse<AuthResponseDto>> GoogleLoginAsync(string idToken)
        {
            // 1. Validate Google token
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
                return ApiResponse<AuthResponseDto>.Failure("Google token غير صالح");
            }

            // 2. Find or create user
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

            // 3. Check if account is active
            if (!user.IsActive)
                return ApiResponse<AuthResponseDto>.Failure("هذا الحساب غير نشط");

            // 4. Generate token and return success
            var authResponse = await GenerateTokenAsync(user);
            return ApiResponse<AuthResponseDto>.Success(authResponse);
        }

        public async Task<ApiResponse<string>> ConfirmEmailAsync(string userId, string token)
        {
            // 1. Find user
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.Failure("المستخدم غير موجود");

            // 2. Confirm email
            var result = await userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse<string>.Failure(errors);
            }

            // 3. Return success
            return ApiResponse<string>.Success("تم تأكيد البريد الإلكتروني بنجاح");
        }

        public Task<bool> LogoutAsync(string userId)
        {
            // JWT stateless — logout is handled by client by deleting token
            // If you need blacklist, add Redis here
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