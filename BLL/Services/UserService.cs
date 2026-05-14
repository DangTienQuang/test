using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly AutoWashDbContext _context;

        public UserService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDTO> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                    .ThenInclude(cp => cp.Tier)
                .Include(u => u.Vehicles)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) throw new Exception("User not found.");

            return new UserProfileDTO
            {
                UserId = user.UserId,
                FullName = user.CustomerProfile?.FullName,
                PhoneNumber = user.PhoneNumber,
                TierName = user.CustomerProfile?.Tier?.TierName,
                ChurnScore = user.CustomerProfile?.ChurnScore ?? 0,
                Vehicles = user.Vehicles.Select(v => new VehicleDTO
                {
                    LicensePlate = v.LicensePlate,
                    VehicleType = v.VehicleType,
                    Brand = v.Brand
                }).ToList()
            };
        }

        public async Task<UserProfileDTO> UpdateProfileAsync(int userId, UpdateProfileDTO request)
        {
            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (profile == null) throw new Exception("Profile not found.");

            profile.FullName = request.FullName;
            profile.Email = request.Email;
            profile.AvatarUrl = request.AvatarUrl;

            await _context.SaveChangesAsync();
            return await GetProfileAsync(userId);
        }

        public async Task<PaginatedResponseDTO<UserProfileDTO>> GetAllUsersAsync(int pageIndex, int pageSize, string? searchName, string? searchPhone, string? searchPlate, int? tierId, string? status)
        {
            var query = _context.Users
                .Include(u => u.CustomerProfile)
                    .ThenInclude(cp => cp.Tier)
                .Include(u => u.Vehicles)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(u => u.CustomerProfile.FullName.Contains(searchName));
            }

            if (!string.IsNullOrEmpty(searchPhone))
            {
                query = query.Where(u => u.PhoneNumber.Contains(searchPhone));
            }

            if (!string.IsNullOrEmpty(searchPlate))
            {
                query = query.Where(u => u.Vehicles.Any(v => v.LicensePlate.Contains(searchPlate)));
            }

            if (tierId.HasValue)
            {
                query = query.Where(u => u.CustomerProfile.TierId == tierId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(u => u.Status == status);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserProfileDTO
                {
                    UserId = u.UserId,
                    FullName = u.CustomerProfile.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Email = u.CustomerProfile.Email,
                    AvatarUrl = u.CustomerProfile.AvatarUrl,
                    TierName = u.CustomerProfile.Tier.TierName,
                    ChurnScore = u.CustomerProfile.ChurnScore,
                    Status = u.Status,
                    Vehicles = u.Vehicles.Select(v => new VehicleDTO
                    {
                        LicensePlate = v.LicensePlate,
                        VehicleType = v.VehicleType,
                        Brand = v.Brand
                    }).ToList()
                })
                .ToListAsync();

            return new PaginatedResponseDTO<UserProfileDTO>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = users
            };
        }

        public async Task<UserProfileDTO> GetUserByIdAsync(int userId)
        {
            return await GetProfileAsync(userId);
        }

        public async Task<bool> ChangeUserStatusAsync(int userId, string status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}