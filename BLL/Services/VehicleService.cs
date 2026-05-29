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
                .Where(v => v.UserId == userId && !v.IsDeleted)
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
            var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(t => t.Id == request.VehicleTypeId);
            if (vehicleType == null) throw new BadRequestException("Loại xe không hợp lệ.");

            if (vehicleType.Name.Contains("Khác", StringComparison.OrdinalIgnoreCase) ||
                vehicleType.Name.Contains("Other", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.RegistrationPhotoUrl))
                {
                    throw new BadRequestException("Bạn bắt buộc phải tải lên hình ảnh thực tế của xe khi chọn loại xe Khác.");
                }

                if (string.IsNullOrWhiteSpace(request.UserNote))
                {
                    throw new BadRequestException("Vui lòng để lại ghi chú tên dòng xe của bạn để chúng tôi hỗ trợ cập nhật.");
                }
            }

            var vehicleCount = await _context.Vehicles.CountAsync(v => v.UserId == userId && !v.IsDeleted);
            if (vehicleCount >= 5)
            {
                throw new BadRequestException("Hồ sơ cá nhân chỉ được liên kết tối đa 5 xe. Vui lòng liên hệ bộ phận CSKH nếu bạn có nhu cầu rửa đội xe lớn.");
            }

            var normalizedPlate = NormalizeLicensePlate(request.LicensePlate);

            var existingVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == normalizedPlate);
            if (existingVehicle != null)
            {
                if (!existingVehicle.IsDeleted)
                {
                    throw new BadRequestException("Biển số xe này đã tồn tại trong hệ thống.");
                }

                // If it is deleted, restore it and update ownership/details
                existingVehicle.IsDeleted = false;
                existingVehicle.UserId = userId;
                existingVehicle.VehicleTypeId = request.VehicleTypeId;
                existingVehicle.RegistrationPhotoUrl = request.RegistrationPhotoUrl;
                existingVehicle.UserNote = request.UserNote;
            }
            else
            {
                var vehicle = new Vehicle
                {
                    LicensePlate = normalizedPlate,
                    VehicleTypeId = request.VehicleTypeId,
                    UserId = userId,
                    RegistrationPhotoUrl = request.RegistrationPhotoUrl,
                    UserNote = request.UserNote
                };

                _context.Vehicles.Add(vehicle);
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<AdminOtherVehicleDTO>> GetOtherVehiclesAsync()
        {
            return await _context.Vehicles
                .Include(v => v.VehicleType)
                .Include(v => v.User)
                    .ThenInclude(u => u.CustomerProfile)
                .Where(v => (!v.IsDeleted) && (v.VehicleType.Name.Contains("Khác") || v.VehicleType.Name.Contains("Other")))
                .Select(v => new AdminOtherVehicleDTO
                {
                    LicensePlate = v.LicensePlate,
                    VehicleTypeId = v.VehicleTypeId,
                    VehicleTypeName = v.VehicleType.Name,
                    UserId = v.UserId,
                    OwnerName = v.User.CustomerProfile != null ? v.User.CustomerProfile.FullName : null,
                    OwnerPhone = v.User != null ? v.User.PhoneNumber : null,
                    RegistrationPhotoUrl = v.RegistrationPhotoUrl,
                    UserNote = v.UserNote
                }).ToListAsync();
        }

        public async Task<bool> UpdateVehicleTypeByAdminAsync(string licensePlate, int newVehicleTypeId)
        {
            licensePlate = NormalizeLicensePlate(Uri.UnescapeDataString(licensePlate));

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && !v.IsDeleted);
            if (vehicle == null) throw new NotFoundException("Không tìm thấy phương tiện.");

            var typeExists = await _context.VehicleTypes.AnyAsync(t => t.Id == newVehicleTypeId);
            if (!typeExists) throw new BadRequestException("Loại xe mới không hợp lệ.");

            vehicle.VehicleTypeId = newVehicleTypeId;
            // Optionally clear the RegistrationPhotoUrl and UserNote if they are no longer "Other"
            // vehicle.RegistrationPhotoUrl = null;
            // vehicle.UserNote = null;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateVehicleAsync(int userId, string licensePlate, UpdateVehicleDTO request)
        {
            licensePlate = NormalizeLicensePlate(Uri.UnescapeDataString(licensePlate));

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && v.UserId == userId && !v.IsDeleted);
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

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && v.UserId == userId && !v.IsDeleted);
            if (vehicle == null) throw new NotFoundException("Không tìm thấy phương tiện hoặc bạn không có quyền xóa xe này.");

            vehicle.IsDeleted = true;
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
                .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && !v.IsDeleted);

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