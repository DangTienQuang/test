using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using BLL.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly AutoWashDbContext _context;

        private readonly IEmailService _emailService;
        private readonly IPhotoService _photoService;
        public VehicleService(AutoWashDbContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        public async Task<List<VehicleDTO>> GetMyVehiclesAsync(int userId)
        {
            return await _context.Vehicles
                .Include(v => v.VehicleType)
                .Include(v => v.CarModelEntity)
                .Where(v => v.UserId == userId && !v.IsDeleted)
                .Select(v => new VehicleDTO
                {
                    LicensePlate = v.LicensePlate,
                    VehicleTypeId = v.VehicleTypeId,
                    VehicleType = v.VehicleType.Name,
                    RegistrationPhotoUrl = v.RegistrationPhotoUrl,
                    CarModel = v.CarModelId.HasValue ? v.CarModelEntity.Name : v.CarModel
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

            string finalPhotoUrl = request.RegistrationPhotoUrl;

            if (request.PhotoFile != null && request.PhotoFile.Length > 0)
            {
                finalPhotoUrl = await _photoService.UploadImageAsync(request.PhotoFile);
            }

            if (vehicleType.Name.Contains("Khác", StringComparison.OrdinalIgnoreCase) ||
                vehicleType.Name.Contains("Other", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(finalPhotoUrl))
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

            int? finalCarModelId = null;
            string? finalCarModel = null;

            if (request.CarModelId.HasValue)
            {
                var carModelExists = await _context.CarModels.AnyAsync(c => c.Id == request.CarModelId.Value && c.IsActive);
                if (!carModelExists)
                    throw new BadRequestException("Dòng xe bạn chọn không tồn tại hoặc đã ngừng hỗ trợ.");

                finalCarModelId = request.CarModelId.Value;
                finalCarModel = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.CarModel))
                    throw new BadRequestException("Vui lòng nhập tên dòng xe của bạn khi chọn mục 'Khác'.");

                finalCarModelId = null;
                finalCarModel = request.CarModel.Trim();
            }

            var existingVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == normalizedPlate);
            if (existingVehicle != null)
            {
                if (!existingVehicle.IsDeleted)
                {
                    throw new BadRequestException("Biển số xe này đã tồn tại trong hệ thống.");
                }

                existingVehicle.IsDeleted = false;
                existingVehicle.UserId = userId;
                existingVehicle.VehicleTypeId = request.VehicleTypeId;
                existingVehicle.RegistrationPhotoUrl = finalPhotoUrl;
                existingVehicle.UserNote = request.UserNote;
                existingVehicle.CarModelId = finalCarModelId;
                existingVehicle.CarModel = finalCarModel;
            }
            else
            {
                var vehicle = new Vehicle
                {
                    LicensePlate = normalizedPlate,
                    VehicleTypeId = request.VehicleTypeId,
                    UserId = userId,
                    RegistrationPhotoUrl = finalPhotoUrl,
                    UserNote = request.UserNote,
                    CarModelId = finalCarModelId,
                    CarModel = finalCarModel
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
                    UserNote = v.UserNote,
                    CarModel = v.CarModel
                }).ToListAsync();
        }

        public async Task<bool> ApproveNewVehicleTypeAsync(string licensePlate, ApproveVehicleTypeRequestDTO request)
        {
            licensePlate = NormalizeLicensePlate(Uri.UnescapeDataString(licensePlate));

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.VehicleType)
                    .Include(v => v.User)
                    .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && !v.IsDeleted);

                if (vehicle == null) throw new NotFoundException("Không tìm thấy phương tiện.");

                if (!vehicle.VehicleType.Name.Contains("Khác", StringComparison.OrdinalIgnoreCase) &&
                    !vehicle.VehicleType.Name.Contains("Other", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BadRequestException("Phương tiện này không nằm trong danh sách yêu cầu chờ duyệt loại xe.");
                }

                var finalTypeName = string.IsNullOrWhiteSpace(request.CustomizedTypeName)
                    ? vehicle.UserNote
                    : request.CustomizedTypeName;

                if (string.IsNullOrWhiteSpace(finalTypeName))
                {
                     throw new BadRequestException("Tên loại xe không được để trống. Vui lòng cung cấp tên loại xe.");
                }

                finalTypeName = finalTypeName.Trim();
                if (finalTypeName.Length > 50)
                {
                    throw new BadRequestException("Tên loại xe không được vượt quá 50 ký tự.");
                }
                var description = string.IsNullOrWhiteSpace(request.Description)
                    ? "Approved from user request"
                    : request.Description.Trim();

                var existingType = await _context.VehicleTypes
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == finalTypeName.ToLower());

                int finalTypeId;

                if (existingType != null)
                {
                    finalTypeId = existingType.Id;
                }
                else
                {
                    var newType = new VehicleType
                    {
                        Name = finalTypeName,
                        Description = description
                    };
                    _context.VehicleTypes.Add(newType);
                    await _context.SaveChangesAsync();
                    finalTypeId = newType.Id;
                }

                vehicle.VehicleTypeId = finalTypeId;
                vehicle.UserNote = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (vehicle.User != null && !string.IsNullOrWhiteSpace(vehicle.User.Email))
                {
                    var subject = "Yêu cầu thêm loại xe mới đã được duyệt";
                    var message = $"Chào bạn,<br/><br/>Yêu cầu thêm loại xe cho phương tiện mang biển số <b>{vehicle.LicensePlate}</b> của bạn đã được quản trị viên duyệt thành công. Loại xe của bạn hiện tại là <b>{finalTypeName}</b>.<br/><br/>Trân trọng,<br/>Đội ngũ AutoWashPro.";
                    _ = Task.Run(() => _emailService.SendEmailAsync(vehicle.User.Email, subject, message));
                }

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RejectNewVehicleTypeAsync(string licensePlate)
        {
            licensePlate = NormalizeLicensePlate(Uri.UnescapeDataString(licensePlate));

            var vehicle = await _context.Vehicles
                .Include(v => v.VehicleType)
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && !v.IsDeleted);

            if (vehicle == null) throw new NotFoundException("Không tìm thấy phương tiện.");

            if (!vehicle.VehicleType.Name.Contains("Khác", StringComparison.OrdinalIgnoreCase) &&
                !vehicle.VehicleType.Name.Contains("Other", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Phương tiện này không nằm trong danh sách yêu cầu chờ duyệt loại xe.");
            }

            vehicle.IsDeleted = true;
            await _context.SaveChangesAsync();

            if (vehicle.User != null && !string.IsNullOrWhiteSpace(vehicle.User.Email))
            {
                var subject = "Yêu cầu thêm phương tiện bị từ chối";
                var message = $"Chào bạn,<br/><br/>Yêu cầu thêm phương tiện mang biển số <b>{vehicle.LicensePlate}</b> của bạn đã bị từ chối do thông tin loại xe không hợp lệ. Vui lòng đăng ký lại phương tiện với thông tin chính xác.<br/><br/>Trân trọng,<br/>Đội ngũ AutoWashPro.";
                _ = Task.Run(() => _emailService.SendEmailAsync(vehicle.User.Email, subject, message));
            }

            return true;
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

            var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(t => t.Id == request.VehicleTypeId);
            if (vehicleType == null) throw new BadRequestException("Loại xe không hợp lệ.");

            string finalPhotoUrl = vehicle.RegistrationPhotoUrl;
            if (request.PhotoFile != null && request.PhotoFile.Length > 0)
            {
                finalPhotoUrl = await _photoService.UploadImageAsync(request.PhotoFile);
            }

            if (vehicleType.Name.Contains("Khác", StringComparison.OrdinalIgnoreCase) ||
                vehicleType.Name.Contains("Other", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(finalPhotoUrl))
                {
                    throw new BadRequestException("Bạn bắt buộc phải tải lên hình ảnh thực tế của xe khi chọn loại xe Khác.");
                }

                if (string.IsNullOrWhiteSpace(request.UserNote) && string.IsNullOrWhiteSpace(vehicle.UserNote))
                {
                    throw new BadRequestException("Vui lòng để lại ghi chú tên dòng xe của bạn để chúng tôi hỗ trợ cập nhật.");
                }
            }

            int? finalCarModelId = null;
            string? finalCarModel = null;

            if (request.CarModelId.HasValue)
            {
                var carModelExists = await _context.CarModels.AnyAsync(c => c.Id == request.CarModelId.Value && c.IsActive);
                if (!carModelExists)
                    throw new BadRequestException("Dòng xe bạn chọn không tồn tại hoặc đã ngừng hỗ trợ.");

                finalCarModelId = request.CarModelId.Value;
                finalCarModel = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.CarModel))
                    throw new BadRequestException("Vui lòng nhập tên dòng xe của bạn khi chọn mục 'Khác'.");

                finalCarModelId = null;
                finalCarModel = request.CarModel.Trim();
            }

            vehicle.VehicleTypeId = request.VehicleTypeId;
            vehicle.RegistrationPhotoUrl = finalPhotoUrl;

            if (!string.IsNullOrWhiteSpace(request.UserNote))
            {
                vehicle.UserNote = request.UserNote;
            }

            vehicle.CarModelId = finalCarModelId;
            vehicle.CarModel = finalCarModel;

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
                .Where(b => b.LicensePlate == licensePlate
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
                ScheduledTime = activeBooking?.ScheduledTime,
                CarModel = vehicle.CarModel
            };
        }
    }
}