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
                    TotalLoyaltyPoints = 0
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
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new AuthResponseDTO
            {
                UserId = user.UserId,
                PhoneNumber = user.PhoneNumber,
                FullName = user.CustomerProfile?.FullName,
                Role = user.Role,
                Token = tokenHandler.WriteToken(token)
            };
        }
    }
}