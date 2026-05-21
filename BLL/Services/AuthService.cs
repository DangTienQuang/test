using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly AutoWashDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AutoWashDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            if (existingUser != null) throw new Exception("Số điện thoại này đã được đăng ký.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var defaultTier = await _context.Tiers.FirstOrDefaultAsync(t => t.MinAccumulatedPoints == 0);

                if (defaultTier == null)
                {
                    defaultTier = new Tier
                    {
                        TierName = "Standard",
                        PointMultiplier = 1.0,
                        BookingWindowDays = 7,
                        MinAccumulatedPoints = 0
                    };
                    _context.Tiers.Add(defaultTier);
                    await _context.SaveChangesAsync();
                }

                var user = new User
                {
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRoles.Customer,
                    Status = UserStatuses.Active 
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var profile = new CustomerProfile
                {
                    UserId = user.UserId,
                    FullName = request.FullName,
                    TierId = defaultTier.TierId,
                    ChurnScore = 0
                };
                _context.CustomerProfiles.Add(profile);

                var wallet = new Wallet
                {
                    UserId = user.UserId,
                    Balance = 0,
                    Status = "Active"
                };
                _context.Wallets.Add(wallet);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return await LoginAsync(new LoginDTO { PhoneOrEmail = request.PhoneNumber, Password = request.Password });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<AuthResponseDTO> LoginAsync(LoginDTO request)
        {
            var loginInput = request.PhoneOrEmail.Trim().ToLower();
            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.PhoneNumber == loginInput || (u.Email != null && u.Email.ToLower() == loginInput));

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Số điện thoại/Email hoặc mật khẩu không chính xác.");

            if (user.Status != "Active")
                throw new Exception("Tài khoản đã bị khóa hoặc không hoạt động.");

            var token = CreateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                UserId = user.UserId,
                PhoneNumber = user.PhoneNumber,
                FullName = user.CustomerProfile?.FullName,
                Role = user.Role,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO request)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null) throw new Exception("Access token không hợp lệ.");

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) throw new Exception("Token không chứa thông tin User.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new Exception("Refresh token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");

            var newAccessToken = CreateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                UserId = user.UserId,
                PhoneNumber = user.PhoneNumber,
                FullName = user.CustomerProfile?.FullName,
                Role = user.Role,
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("Không tìm thấy người dùng.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new Exception("Mật khẩu cũ không chính xác.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        private string CreateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Key"])),
                ValidateLifetime = false 
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Token không hợp lệ.");

            return principal;
        }
    }
}