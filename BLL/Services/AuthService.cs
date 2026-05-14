using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
            if (existingUser != null) throw new Exception("Phone number already exists.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new User
                {
                    PhoneNumber = request.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = "Customer",
                    Status = "Active"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var standardTier = await _context.Tiers.FirstOrDefaultAsync(t => t.TierName == "Standard");
                if (standardTier == null) throw new Exception("System error: Standard tier is missing in the database.");

                var profile = new CustomerProfile
                {
                    UserId = user.UserId,
                    FullName = request.FullName,
                    TierId = standardTier.TierId,
                    ChurnScore = 0
                };
                _context.CustomerProfiles.Add(profile);

                var wallet = new Wallet
                {
                    UserId = user.UserId,
                    MainBalance = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(wallet);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AuthResponseDTO
                {
                    UserId = user.UserId,
                    PhoneNumber = user.PhoneNumber,
                    FullName = profile.FullName,
                    Role = user.Role
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginDTO request)
        {
            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new Exception("Invalid phone number or password.");

            if (user.Status != "Active")
                throw new Exception("Account is not active.");

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
                Expires = DateTime.UtcNow.AddMinutes(30), // 30 minutes
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            user.RefreshToken = Guid.NewGuid().ToString();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                UserId = user.UserId,
                PhoneNumber = user.PhoneNumber,
                FullName = user.CustomerProfile?.FullName,
                Role = user.Role,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = user.RefreshToken
            };
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO request)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null) throw new Exception("Invalid access token or refresh token");

            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) throw new Exception("Invalid user.");

            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new Exception("Invalid access token or refresh token");
            }

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

            user.RefreshToken = Guid.NewGuid().ToString();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return new AuthResponseDTO
            {
                UserId = user.UserId,
                PhoneNumber = user.PhoneNumber,
                FullName = user.CustomerProfile?.FullName,
                Role = user.Role,
                Token = tokenHandler.WriteToken(token),
                RefreshToken = user.RefreshToken
            };
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDTO request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                throw new Exception("Incorrect current password.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            if (user == null) throw new Exception("Phone number is not registered.");

            // MOCK OTP SENDING
            var otp = new Random().Next(100000, 999999).ToString();
            Console.WriteLine($"[MOCK OTP SERVICE] OTP for {request.PhoneNumber} is: {otp}");

            // In a real application, you would save this OTP to the database or memory cache and send it via SMS
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // Set to true and provide valid audience in real scenario
                ValidateIssuer = false, // Set to true and provide valid issuer in real scenario
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "")),
                ValidateLifetime = false // Here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}