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
                    VehicleType = v.VehicleType
                }).ToList()
            };
        }

        public async Task<bool> AddVehicleAsync(int userId, CreateVehicleDTO request)
        {
            var vehicleCount = await _context.Vehicles.CountAsync(v => v.UserId == userId);
            if (vehicleCount >= 3) throw new Exception("Maximum 3 vehicles allowed per user.");

            var existingVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == request.LicensePlate);
            if (existingVehicle != null) throw new Exception("License plate already exists.");

            var vehicle = new Vehicle
            {
                LicensePlate = request.LicensePlate,
                VehicleType = request.VehicleType,
                UserId = userId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}