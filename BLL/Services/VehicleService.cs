using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.BLL.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly AutoWashDbContext _context;

        public VehicleService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleDTO>> GetVehiclesAsync(int userId)
        {
            return await _context.Vehicles
                .Where(v => v.UserId == userId)
                .Select(v => new VehicleDTO
                {
                    LicensePlate = v.LicensePlate,
                    VehicleType = v.VehicleType,
                    Brand = v.Brand
                })
                .ToListAsync();
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
                Brand = request.Brand,
                UserId = userId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateVehicleAsync(int userId, string licensePlate, UpdateVehicleDTO request)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.UserId == userId && v.LicensePlate == licensePlate);
            if (vehicle == null) throw new Exception("Vehicle not found.");

            vehicle.VehicleType = request.VehicleType;
            vehicle.Brand = request.Brand;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteVehicleAsync(int userId, string licensePlate)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.UserId == userId && v.LicensePlate == licensePlate);
            if (vehicle == null) throw new Exception("Vehicle not found.");

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
