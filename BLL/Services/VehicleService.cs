using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Exceptions;

namespace AutoWashPro.BLL.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly AutoWashDbContext _context;

        public VehicleService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleDTO>> GetMyVehiclesAsync(int userId)
        {
            return await _context.Vehicles
                .Include(v => v.VehicleType)
                .Where(v => v.UserId == userId)
                .Select(v => new VehicleDTO
                {
                    LicensePlate = v.LicensePlate,
                    VehicleType = v.VehicleType.Name
                }).ToListAsync();
        }

        private string NormalizeLicensePlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
            return new string(plate.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
        }

        public async Task<bool> AddVehicleAsync(int userId, CreateVehicleDTO request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.Role == "Customer")
            {
                // In the future, B2B logic (e.g. AccountType == "Business") would bypass this completely.
                // For now, we removed the strict 5 vehicle limit for B2B requests and let them add freely,
                // but kept it as an example for basic customers. If the user wants it totally gone:
                // We just remove the check. Given the request: "Bỏ giới hạn 5 chiếc xe, giải quyết bài toán khách hàng Công ty",
                // we will disable the strict hardcoded limit entirely to support B2B.
            }

            // Allow unlimited vehicles to solve B2B/Fleet management pain point
            // var vehicleCount = await _context.Vehicles.CountAsync(v => v.UserId == userId);
            // if (vehicleCount >= 5 && user?.Role == "Customer") throw new BadRequestException("Bạn chỉ được thêm tối đa 5 xe.");

            var normalizedPlate = NormalizeLicensePlate(request.LicensePlate);

            var existingVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == normalizedPlate);
            if (existingVehicle != null) throw new BadRequestException("Biển số xe này đã tồn tại trong hệ thống.");

            var typeExists = await _context.VehicleTypes.AnyAsync(t => t.Id == request.VehicleTypeId);
            if (!typeExists) throw new BadRequestException("Loại xe không hợp lệ.");

            var vehicle = new Vehicle
            {
                LicensePlate = normalizedPlate,
                VehicleTypeId = request.VehicleTypeId,
                UserId = userId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateVehicleAsync(int userId, string licensePlate, UpdateVehicleDTO request)
        {
            licensePlate = NormalizeLicensePlate(Uri.UnescapeDataString(licensePlate));

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && v.UserId == userId);
            if (vehicle == null) throw new NotFoundException("Không tìm thấy phương tiện hoặc bạn không có quyền thao tác trên xe này.");

            var typeExists = await _context.VehicleTypes.AnyAsync(t => t.Id == request.VehicleTypeId);
            if (!typeExists) throw new BadRequestException("Loại xe không hợp lệ.");

            vehicle.VehicleTypeId = request.VehicleTypeId;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteVehicleAsync(int userId, string licensePlate)
        {
            licensePlate = NormalizeLicensePlate(Uri.UnescapeDataString(licensePlate));

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && v.UserId == userId);
            if (vehicle == null) throw new NotFoundException("Không tìm thấy phương tiện hoặc bạn không có quyền xóa xe này.");

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<VehicleRecognitionDTO> RecognizeVehicleAsync(string licensePlate)
        {
            licensePlate = NormalizeLicensePlate(Uri.UnescapeDataString(licensePlate));

            var vehicle = await _context.Vehicles
                .Include(v => v.VehicleType)
                .Include(v => v.User)
                    .ThenInclude(u => u.CustomerProfile)
                        .ThenInclude(cp => cp.Tier)
                .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate);

            if (vehicle == null)
                throw new NotFoundException("Biển số xe chưa được đăng ký trên hệ thống.");

            if (vehicle.User == null || vehicle.User.CustomerProfile == null)
                throw new BadRequestException("Lỗi dữ liệu: Xe không có thông tin chủ sở hữu.");

            var today = DateTime.UtcNow.Date;

            var activeBooking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .Where(b => b.BookingDetails.Any(bd => bd.LicensePlate == licensePlate)
                         && (b.Status == "Pending" || b.Status == "CheckedIn")
                         && b.ScheduledTime.Date == today)
                .OrderBy(b => b.ScheduledTime)
                .FirstOrDefaultAsync();

            return new VehicleRecognitionDTO
            {
                LicensePlate = vehicle.LicensePlate,
                VehicleType = vehicle.VehicleType?.Name ?? "N/A",
                OwnerName = vehicle.User.CustomerProfile.FullName,
                OwnerPhone = vehicle.User.PhoneNumber,
                TierName = vehicle.User.CustomerProfile.Tier?.TierName ?? "N/A",
                HasActiveBooking = activeBooking != null,
                ActiveBookingId = activeBooking?.BookingId,
                ScheduledTime = activeBooking?.ScheduledTime
            };
        }
    }
}