using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using BLL.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.BLL.Services
{
    public class BookingService : IBookingService
    {
        private readonly AutoWashDbContext _context;
        private readonly IWalletService _walletService;
        private readonly ITierService _tierService;
        private readonly IEmailService _emailService;
        private readonly IVoucherService _voucherService;
        private readonly IVoucherCampaignService _voucherCampaignService;

        public BookingService(
            AutoWashDbContext context,
            IWalletService walletService,
            ITierService tierService,
            IEmailService emailService,
            IVoucherService voucherService,
            IVoucherCampaignService voucherCampaignService)
        {
            _context = context;
            _walletService = walletService;
            _tierService = tierService;
            _emailService = emailService;
            _voucherService = voucherService;
            _voucherCampaignService = voucherCampaignService;
        }

        public async Task<List<TimeSlotResponseDTO>> GetAvailableSlotsAsync(int userId, CheckAvailableSlotsRequestDTO request)
        {
            var userProfile = await _context.CustomerProfiles.Include(cp => cp.Tier).FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (userProfile == null || userProfile.Tier == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy thông tin hạng thành viên.");

            // 1. SỬA LỖI MÚI GIỜ (Dùng cho cả Docker/Linux/Windows)
            TimeZoneInfo vnTimeZone;
            try { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch (TimeZoneNotFoundException) { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }

            DateTime todayInVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone).Date;
            TimeSpan currentTimeInVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone).TimeOfDay;

            var maxDate = todayInVN.AddDays(userProfile.Tier.BookingWindowDays);

            if (request.TargetDate.Date < todayInVN || request.TargetDate.Date > maxDate)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Hạng {userProfile.Tier.TierName} chỉ được đặt trước từ hôm nay đến ngày {maxDate:dd/MM/yyyy}.");
            }

            // 2. TÍNH TỔNG TRỌNG LƯỢNG (WEIGHT) CỦA GIỎ HÀNG KHÁCH VỪA CHỌN
            int totalRequestWeight = 0;
            if (request.ServiceIds != null && request.ServiceIds.Any())
            {
                var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(vt => vt.Id == request.VehicleTypeId);
                var baseWeight = vehicleType?.BaseWeight ?? 0;

                foreach (var serviceId in request.ServiceIds)
                {
                    var servicePrice = await _context.ServicePrices
                        .FirstOrDefaultAsync(sp => sp.ServiceId == serviceId
                                                && sp.VehicleTypeId == request.VehicleTypeId
                                                && sp.BranchId == request.BranchId);

                    var actualWeight = servicePrice?.CapacityWeight > 0 ? servicePrice.CapacityWeight : baseWeight;
                    if (actualWeight > totalRequestWeight) totalRequestWeight = actualWeight;
                }
            }

            var allSlots = await _context.TimeSlots
                .Where(s => s.BranchId == request.BranchId)
                .OrderBy(s => s.StartTime).ToListAsync();
            var response = new List<TimeSlotResponseDTO>();

            var dailyCapacities = await _context.DailySlotCapacities
                .Where(dc => dc.BranchId == request.BranchId && dc.Date == request.TargetDate.Date)
                .ToDictionaryAsync(dc => dc.SlotId, dc => dc.BookedWeight);

            bool isVip = userProfile.Tier.TierName.ToLower() == "gold" || userProfile.Tier.TierName.ToLower() == "platinum";

            // 3. VÒNG LẶP KIỂM TRA TỪNG SLOT
            foreach (var slot in allSlots)
            {
                var slotDto = new TimeSlotResponseDTO
                {
                    SlotId = slot.SlotId,
                    TimeRange = $"{slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}",
                    IsAvailable = true,
                    Reason = "Trống"
                };

                if (slot.IsVipOnly && !isVip)
                {
                    slotDto.IsAvailable = false;
                    slotDto.Reason = "Chỉ dành cho VIP";
                }

                // Chặn slot quá giờ so với giờ Việt Nam
                if (request.TargetDate.Date == todayInVN && slot.StartTime < currentTimeInVN)
                {
                    slotDto.IsAvailable = false;
                    slotDto.Reason = "Đã qua giờ";
                }

                int bookedWeight = dailyCapacities.TryGetValue(slot.SlotId, out int weight) ? weight : 0;

                // --- LOGIC AI SỨC CHỨA ---
                // Lượng đã đặt + Lượng khách ĐANG ĐỊNH ĐẶT > Sức chứa tối đa
                if (bookedWeight + totalRequestWeight > slot.MaxCapacity)
                {
                    slotDto.IsAvailable = false;
                    // Nếu khách có add xe vào giỏ thì báo "Không đủ chỗ cho dịch vụ", nếu không thì báo "Đã kín"
                    slotDto.Reason = totalRequestWeight > 0 ? "Không đủ sức chứa cho giỏ hàng của bạn" : "Đã kín chỗ";
                }

                response.Add(slotDto);
            }

            return response;
        }

        public async Task<List<BookingResponseDTO>> GetAllBookingsByDateAsync(DateTime targetDate)
        {
            var bookings = await _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Service)
                .Where(b => b.ScheduledTime.Date == targetDate.Date)
                .OrderBy(b => b.ScheduledTime)
                .ToListAsync();

            return bookings.Select(b => new BookingResponseDTO
            {
                BookingId = b.BookingId,
                LicensePlate = b.LicensePlate,
                ServiceNames = b.BookingDetails.Select(d => d.Service.ServiceName).ToList(),
                ScheduledTime = b.ScheduledTime,
                Status = b.Status,
                OriginalPrice = b.OriginalPrice,
                PointDiscountAmount = b.PointDiscountAmount,
                VoucherDiscountAmount = b.VoucherDiscountAmount,
                FinalAmount = b.FinalAmount
            }).ToList();
        }

        public async Task<BookingResponseDTO> GetBookingByIdAsync(int userId, int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Service)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

            if (booking == null)
                throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy chi tiết lịch hẹn hoặc bạn không có quyền xem.");

            return new BookingResponseDTO
            {
                BookingId = booking.BookingId,
                LicensePlate = booking.LicensePlate,
                ServiceNames = booking.BookingDetails.Select(d => d.Service.ServiceName).ToList(),
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status,
                OriginalPrice = booking.OriginalPrice,
                PointDiscountAmount = booking.PointDiscountAmount,
                VoucherDiscountAmount = booking.VoucherDiscountAmount,
                FinalAmount = booking.FinalAmount
            };
        }

        private string NormalizeLicensePlate(string plate)
        {
            if (string.IsNullOrWhiteSpace(plate)) return string.Empty;
            return new string(plate.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
        }

        public async Task<List<BookingResponseDTO>> GetBookingsByLicensePlateAsync(string licensePlate)
        {
            var normalizedPlate = NormalizeLicensePlate(licensePlate);
            if (string.IsNullOrEmpty(normalizedPlate))
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Biển số xe không hợp lệ.");

            var bookings = await _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Service)
                .Include(b => b.User)
                    .ThenInclude(u => u.CustomerProfile)
                .Where(b => b.LicensePlate.Replace("-", "").Replace(".", "").Replace(" ", "").ToUpper() == normalizedPlate)
                .OrderByDescending(b => b.ScheduledTime)
                .AsNoTracking()
                .ToListAsync();

            if (bookings.Count == 0)
            {
                return new List<BookingResponseDTO>();
            }

            return bookings.Select(b =>
            {
                return new BookingResponseDTO
                {
                    BookingId = b.BookingId,
                    LicensePlate = b.LicensePlate,
                    ServiceNames = b.BookingDetails.Select(d => d.Service.ServiceName).ToList(),
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status,
                    OriginalPrice = b.OriginalPrice,
                    PointDiscountAmount = b.PointDiscountAmount,
                    VoucherDiscountAmount = b.VoucherDiscountAmount,
                    FinalAmount = b.FinalAmount
                };
            }).ToList();
        }

        public async Task<BookingResponseDTO> UpdateBookingStatusByLicensePlateAsync(string licensePlate, string newStatus)
        {
            // 1. KIỂM TRA DỮ LIỆU ĐẦU VÀO
            var allowedStatuses = new[] { "Pending", "CheckedIn", "Completed", "Cancelled", "Delayed", "CancelledBySystem" };
            if (!allowedStatuses.Contains(newStatus))
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Trạng thái không hợp lệ.");

            var normalizedPlate = NormalizeLicensePlate(licensePlate);
            if (string.IsNullOrEmpty(normalizedPlate))
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Biển số xe không hợp lệ.");

            // 2. TÌM KIẾM DIỆN RỘNG (Tránh hoàn toàn lỗi lệch giờ UTC vs VN)
            // Lấy dư ra 24h trước và sau để đảm bảo không bỏ sót bất kỳ đơn nào do lệch timezone
            var startTime = DateTime.UtcNow.AddHours(-24);
            var endTime = DateTime.UtcNow.AddHours(24);

            var query = await _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Service)
                .Where(b => b.ScheduledTime >= startTime && b.ScheduledTime <= endTime)
                .ToListAsync();

            // 3. LỌC CHÍNH XÁC BIỂN SỐ (In-memory)
            var matches = query.Where(b =>
                NormalizeLicensePlate(b.LicensePlate) == normalizedPlate
            ).ToList();

            if (matches.Count == 0)
            {
                throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Không tìm thấy lịch hẹn nào cho xe {licensePlate} trong khoảng thời gian gần đây.");
            }

            // 4. LỌC CHÍNH XÁC NGÀY HÔM NAY (Giờ Việt Nam)
            var todayInVN = DateTime.UtcNow.ToVnTime().Date;
            var todaysBookings = matches.Where(b => b.ScheduledTime.ToVnTime().Date == todayInVN).ToList();

            if (todaysBookings.Count == 0)
            {
                throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Xe {licensePlate} có lịch, nhưng không phải là lịch của ngày hôm nay ({todayInVN:dd/MM/yyyy}).");
            }

            if (todaysBookings.Count > 1)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Phát hiện nhiều lịch hẹn ({todaysBookings.Count}) cho xe {licensePlate} trong ngày hôm nay. Vui lòng sử dụng mã Booking ID để thao tác.");
            }

            var booking = todaysBookings.First();

            // 5. KIỂM TRA LOGIC CHUYỂN TRẠNG THÁI (Báo lỗi 400 rõ ràng thay vì 404 Not Found)
            if (booking.Status == newStatus)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Trạng thái hiện tại của đơn hàng đã là '{newStatus}' rồi.");
            }

            bool isStatusValid = false;
            if (newStatus == "CheckedIn" && booking.Status == "Pending") isStatusValid = true;
            else if (newStatus == "Completed" && booking.Status == "CheckedIn") isStatusValid = true;
            else if ((newStatus == "Cancelled" || newStatus == "Delayed") && (booking.Status == "Pending" || booking.Status == "CheckedIn")) isStatusValid = true;

            if (!isStatusValid)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Không thể chuyển trạng thái từ '{booking.Status}' sang '{newStatus}'.");
            }

            // 6. THỰC THI CẬP NHẬT (Tận dụng logic của hàm UpdateBookingStatusAsync)
            var isUpdated = await UpdateBookingStatusAsync(booking.BookingId, newStatus);

            if (!isUpdated)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Cập nhật trạng thái thất bại do hệ thống.");
            }

            // 7. TRẢ VỀ DTO
            return new BookingResponseDTO
            {
                BookingId = booking.BookingId,
                LicensePlate = normalizedPlate,
                ServiceNames = booking.BookingDetails.Select(d => d.Service.ServiceName).ToList(),
                ScheduledTime = booking.ScheduledTime,
                Status = newStatus,
                OriginalPrice = booking.OriginalPrice,
                PointDiscountAmount = booking.PointDiscountAmount,
                VoucherDiscountAmount = booking.VoucherDiscountAmount,
                FinalAmount = booking.FinalAmount
            };
        }
        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string newStatus)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy lịch hẹn.");

            var allowedStatuses = new[] { "Pending", "CheckedIn", "Completed", "Cancelled", "Delayed", "CancelledBySystem" };
            if (!allowedStatuses.Contains(newStatus)) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Trạng thái không hợp lệ.");

            if ((newStatus == "CheckedIn" || newStatus == "Completed")
                && booking.FinalAmount > 0
                && !await HasCompletedBookingPaymentAsync(booking.BookingId))
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Lịch hẹn chưa thanh toán, không thể check-in hoặc hoàn thành.");

            if (newStatus == "Completed" && booking.Status != "Completed")
            {
                if (booking.UserId > 0)
                {
                    var userProfile = await _context.CustomerProfiles
                        .Include(cp => cp.Tier)
                        .FirstOrDefaultAsync(cp => cp.UserId == booking.UserId);

                    if (userProfile?.Tier != null && booking.FinalAmount > 0)
                    {
                        int pointsEarned = (int)((booking.FinalAmount / PointConstants.VndPerEarnedPoint) * (decimal)userProfile.Tier.PointMultiplier);

                        if (pointsEarned > 0)
                        {
                            await _walletService.AwardCompletionPointsAsync(
                                booking.UserId.Value, pointsEarned, booking.BookingId);
                        }
                    }

                    if (userProfile != null)
                        userProfile.LastVisitDate = DateTime.UtcNow;
                }
            }

            booking.Status = newStatus;
            booking.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (newStatus == "Completed" && booking.UserId.HasValue)
            {
                await _voucherCampaignService.ProcessMilestoneCampaignsAsync(booking.UserId.Value);
            }

            return true;
        }

        public async Task<CompatibilityDTO> ValidateBookingCompatibilityAsync(int userId, int branchId, int slotId, DateTime targetDate, int? vehicleId, string licensePlate, List<int> serviceIds)
        {
            if (serviceIds == null || serviceIds.Count == 0)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Vui lòng chọn ít nhất 1 dịch vụ.");

            var slot = await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.SlotId == slotId && ts.BranchId == branchId);
            if (slot == null)
                throw new AutoWashPro.BLL.Exceptions.NotFoundException("Khung giờ không hợp lệ.");

            if (slot.IsVipOnly)
            {
                var profile = await _context.CustomerProfiles
                    .Include(p => p.Tier)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null || profile.Tier == null)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Không tìm thấy thông tin hạng thành viên.");

                string tierName = profile.Tier.TierName.ToLower();
                if (tierName != "gold" && tierName != "platinum")
                {
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Khung giờ này là đặc quyền chỉ dành riêng cho thành viên Gold và Platinum.");
                }
            }

            var targetDateTime = targetDate.Date.Add(slot.StartTime);
            if (targetDateTime < DateTime.UtcNow)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Không thể đặt lịch trong quá khứ.");

            int totalCapacityWeight = 0;

            var vehicle = await _context.Vehicles.Include(v => v.VehicleType).FirstOrDefaultAsync(v => v.LicensePlate == licensePlate && v.UserId == userId && !v.IsDeleted);
            if (vehicle == null)
                throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Xe với biển số {licensePlate} không tồn tại trong hồ sơ của bạn.");

            bool hasActiveBooking = await _context.Bookings.AnyAsync(b => b.LicensePlate == licensePlate && (b.Status == "Pending" || b.Status == "CheckedIn"));
            if (hasActiveBooking)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Xe biển số {licensePlate} đang có lịch hẹn chưa hoàn thành. Không thể đặt thêm lịch mới cho xe này.");

            foreach (var serviceId in serviceIds)
            {
                var service = await _context.Services.FindAsync(serviceId);
                if (service == null || !service.IsActive)
                    throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Dịch vụ {serviceId} không tồn tại hoặc đã ngừng kinh doanh.");

                var servicePrice = await _context.ServicePrices.FirstOrDefaultAsync(sp => sp.ServiceId == serviceId && sp.VehicleTypeId == vehicle.VehicleTypeId && sp.BranchId == branchId);
                if (servicePrice == null)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Dịch vụ {serviceId} chưa hỗ trợ cho loại xe này.");

                var actualCapacityWeight = servicePrice.CapacityWeight > 0 ? servicePrice.CapacityWeight : vehicle.VehicleType.BaseWeight;
                if (actualCapacityWeight > totalCapacityWeight) totalCapacityWeight = actualCapacityWeight;
            }

            var dailyCapacity = await _context.DailySlotCapacities.FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.BranchId == branchId && dc.Date == targetDateTime.Date);
            int bookedWeight = dailyCapacity?.BookedWeight ?? 0;
            int remainingCapacity = slot.MaxCapacity - bookedWeight;

            if (bookedWeight + totalCapacityWeight > slot.MaxCapacity)
            {
                return new CompatibilityDTO
                {
                    IsCompatible = false,
                    Message = "Xưởng không đủ sức chứa cho yêu cầu của bạn.",
                    RemainingCapacity = remainingCapacity > 0 ? remainingCapacity : 0,
                    TotalCapacityWeight = totalCapacityWeight,
                    MaxCapacityOfSlot = slot.MaxCapacity
                };
            }

            return new CompatibilityDTO
            {
                IsCompatible = true,
                Message = "Sức chứa hợp lệ.",
                RemainingCapacity = remainingCapacity,
                TotalCapacityWeight = totalCapacityWeight,
                MaxCapacityOfSlot = slot.MaxCapacity
            };
        }
        // File: BLL/Services/BookingService.cs

        public async Task<bool> SendBookingConfirmationEmailAsync(int userId, int bookingId)
        {
            try
            {
                // 1. Lấy thông tin Booking và User. 
                // Phải Include đầy đủ vì context này chạy hoàn toàn độc lập với luồng CreateBooking
                var booking = await _context.Bookings
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.Service)
                    .Include(b => b.User)
                        .ThenInclude(u => u.CustomerProfile)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

                // 2. Validate an toàn
                if (booking == null || string.IsNullOrEmpty(booking.User?.Email))
                {
                    Console.WriteLine($"[Cảnh báo] Không thể gửi mail: Không tìm thấy đơn #{bookingId} hoặc User không có email.");
                    return false;
                }

                var customerName = booking.User.CustomerProfile?.FullName ?? "Quý khách";

                // 3. Build HTML Template (Nghiệp vụ tốn CPU)
                var emailHtml = EmailTemplateBuilder.BuildBookingConfirmationEmail(
                    booking,
                    booking.BookingDetails.ToList(),
                    customerName
                );

                // 4. Gọi Service Gửi mail
                await _emailService.SendEmailAsync(
                    booking.User.Email,
                    $"[SmartWash] Đặt lịch thành công - #{booking.BookingId}",
                    emailHtml
                );

                return true;
            }
            catch (Exception ex)
            {
                // Trong Background Task, KHÔNG ĐƯỢC throw exception làm crash app. 
                // Chỉ ghi Log để Developer trace lỗi.
                Console.WriteLine($"[Lỗi Background Task - Email] Booking #{bookingId}: {ex.Message}");
                return false;
            }
        }
        public async Task<CompatibilityDTO> CheckCompatibilityAsync(int userId, CheckCompatibilityRequestDTO request)
        {
            return await ValidateBookingCompatibilityAsync(userId, request.BranchId, request.SlotId, request.TargetDate, request.VehicleId, request.LicensePlate, request.ServiceIds);
        }

        public async Task<BookingResponseDTO> CreateBookingAsync(int userId, CreateBookingDTO request)
        {
            var compatibility = await ValidateBookingCompatibilityAsync(userId, request.BranchId, request.SlotId, request.ScheduledDate, request.VehicleId, request.LicensePlate, request.ServiceIds);

            if (!compatibility.IsCompatible)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException(compatibility.Message ?? "Xưởng không đủ sức chứa cho yêu cầu của bạn.");
            }

            var slot = await _context.TimeSlots.FindAsync(request.SlotId);
            var targetDateTime = request.ScheduledDate.Date.Add(slot.StartTime);

            var pendingDetails = new List<BookingDetail>();

            var vehicleTypeQuery = await _context.Vehicles
                .Where(v => v.LicensePlate == request.LicensePlate && v.UserId == userId && !v.IsDeleted)
                .Select(v => new { v.LicensePlate, v.VehicleType.BaseWeight, v.VehicleTypeId })
                .FirstOrDefaultAsync();

            if (vehicleTypeQuery == null)
            {
                 throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy phương tiện.");
            }

            var servicePrices = await _context.ServicePrices
                .Include(sp => sp.Service)
                .Where(sp => request.ServiceIds.Contains(sp.ServiceId) && sp.VehicleTypeId == vehicleTypeQuery.VehicleTypeId && sp.BranchId == request.BranchId)
                .ToListAsync();

            decimal totalOriginalPrice = 0;
            int maxCapacityWeight = 0;

            foreach (var serviceId in request.ServiceIds)
            {
                var sp = servicePrices.FirstOrDefault(s => s.ServiceId == serviceId);
                if (sp == null)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Dịch vụ {serviceId} không hỗ trợ cho loại xe này.");

                var actualWeight = sp.CapacityWeight > 0 ? sp.CapacityWeight : vehicleTypeQuery.BaseWeight;
                if (actualWeight > maxCapacityWeight) maxCapacityWeight = actualWeight;

                totalOriginalPrice += sp.Price;

                pendingDetails.Add(new BookingDetail
                {
                    ServiceId = serviceId,
                    Price = sp.Price
                });
            }

            // PHASE 3: Capacity AI
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            var dailyCapacity = await _context.DailySlotCapacities.FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.BranchId == request.BranchId && dc.Date == targetDateTime.Date);
            if (dailyCapacity == null)
            {
                dailyCapacity = new DailySlotCapacity
                {
                    SlotId = slot.SlotId,
                    BranchId = request.BranchId,
                    Date = targetDateTime.Date,
                    BookedWeight = 0
                };
                _context.DailySlotCapacities.Add(dailyCapacity);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
                {
                    _context.Entry(dailyCapacity).State = EntityState.Detached;
                    dailyCapacity = await _context.DailySlotCapacities.FirstAsync(dc => dc.SlotId == slot.SlotId && dc.BranchId == request.BranchId && dc.Date == targetDateTime.Date);
                }
            }
            else
            {
               // Re-fetch to ensure we have the latest version before modifying
               dailyCapacity = await _context.DailySlotCapacities.FirstAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
            }

            if (dailyCapacity.BookedWeight + maxCapacityWeight > slot.MaxCapacity)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Xưởng không đủ sức chứa cho xe này. Vui lòng chọn khung giờ khác.");

            // PHASE 4: Financial Math
            var (voucherDiscount, pointDiscount, pointsUsed, finalAmount, userVoucher) =
                await CalculateBookingPricingAsync(userId, totalOriginalPrice, request.VoucherId, request.PointsToUse, targetDateTime);

            // PHASE 5: Transaction
            var paymentMethod = request.PaymentMethod?.Trim() ?? "Wallet";
            var isPayOsPayment = string.Equals(paymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(paymentMethod, "QR", StringComparison.OrdinalIgnoreCase);
            var isWalletPayment = string.Equals(paymentMethod, "Wallet", StringComparison.OrdinalIgnoreCase);
            if (!isPayOsPayment && !isWalletPayment)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Phương thức thanh toán không hợp lệ. Chỉ hỗ trợ Wallet hoặc QR.");

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0, Status = "Active" };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            if (!isPayOsPayment && wallet.Balance < finalAmount)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Số dư ví không đủ để đặt cọc. Cần: {finalAmount:N0}đ");

            try
            {
                // Update Capacity
                dailyCapacity.BookedWeight += maxCapacityWeight;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Có người khác vừa đặt lịch. Vui lòng thử lại.");
                }

                Transaction? paymentTx = null;
                if (!isPayOsPayment)
                {
                    wallet.Balance -= finalAmount;

                    paymentTx = new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = -finalAmount,
                    TransactionType = "Payment",
                    Description = $"Thanh toán cọc lịch rửa xe lúc {targetDateTime:dd/MM/yyyy HH:mm}"
                    };
                    _context.Transactions.Add(paymentTx);
                }

                // Apply Voucher & Points
                if (userVoucher != null)
                {
                    userVoucher.UsageCount += 1;
                    userVoucher.IsUsed = userVoucher.UsageCount >= userVoucher.Voucher.MaxUsagePerUser;
                    userVoucher.UsedDate = DateTime.UtcNow;
                    userVoucher.LastUsedDate = DateTime.UtcNow;
                    userVoucher.Voucher.CurrentUsageCount += 1;
                }

                if (pointsUsed > 0)
                {
                    await _walletService.DeductSpendablePointsAsync(userId, pointsUsed, "Dùng điểm giảm giá đặt lịch");
                }

                // Create Booking
                var booking = new Booking
                {
                    UserId = userId,
                    VehicleId = request.VehicleId,
                    LicensePlate = request.LicensePlate,
                    CapacityWeight = maxCapacityWeight,
                    VehicleCondition = VehicleCondition.Clean,
                    BranchId = request.BranchId,
                    ScheduledTime = targetDateTime,
                    Status = "Pending",
                    OriginalPrice = totalOriginalPrice,
                    PointsUsed = pointsUsed,
                    PointDiscountAmount = pointDiscount,
                    AppliedVoucherId = request.VoucherId,
                    VoucherDiscountAmount = voucherDiscount,
                    FinalAmount = finalAmount,
                    BookingDetails = pendingDetails,
                    FallbackQrCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() // Generate a random QR string
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                if (paymentTx != null)
                {
                    paymentTx.ReferenceBookingId = booking.BookingId;
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // PHASE 6: Post-Processing
                if (!isPayOsPayment)
                {
                try
                {
                    var user = await _context.Users.Include(u => u.CustomerProfile).FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailHtml = EmailTemplateBuilder.BuildBookingConfirmationEmail(booking, pendingDetails, user.CustomerProfile?.FullName ?? "Quý khách");
                        _ = Task.Run(() => _emailService.SendEmailAsync(user.Email, $"[SmartWash] Đặt lịch thành công - #{booking.BookingId}", emailHtml));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Lỗi gửi mail]: {ex.Message}");
                }

                }

                var serviceNames = await _context.Services.Where(s => request.ServiceIds.Contains(s.ServiceId)).Select(s => s.ServiceName).ToListAsync();

                return new BookingResponseDTO
                {
                    BookingId = booking.BookingId,
                    LicensePlate = booking.LicensePlate,
                    ServiceNames = serviceNames,
                    ScheduledTime = booking.ScheduledTime,
                    Status = booking.Status,
                    OriginalPrice = booking.OriginalPrice,
                    PointDiscountAmount = booking.PointDiscountAmount,
                    VoucherDiscountAmount = booking.VoucherDiscountAmount,
                    FinalAmount = booking.FinalAmount
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BookingPaymentLinkResponseDTO> CreateBookingPaymentLinkAsync(int userId, int bookingId, CreateBookingPaymentLinkDTO request)
        {
            var result = await _walletService.CreatePaymentQrAsync(userId, new PaymentQrRequestDTO
            {
                PaymentType = "BookingPayment",
                BookingId = bookingId,
                CancelUrl = request.CancelUrl,
                ReturnUrl = request.ReturnUrl
            });

            return new BookingPaymentLinkResponseDTO
            {
                BookingId = result.BookingId ?? bookingId,
                Amount = result.Amount,
                OrderCode = result.OrderCode,
                PaymentUrl = result.PaymentUrl
            };
        }

        public async Task<List<BookingResponseDTO>> GetMyBookingsAsync(int userId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(bd => bd.Service)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.ScheduledTime)
                .ToListAsync();

            return bookings.Select(b => new BookingResponseDTO
            {
                BookingId = b.BookingId,
                LicensePlate = b.LicensePlate,
                ServiceNames = b.BookingDetails.Select(d => d.Service.ServiceName).ToList(),
                ScheduledTime = b.ScheduledTime,
                Status = b.Status,
                OriginalPrice = b.OriginalPrice,
                PointDiscountAmount = b.PointDiscountAmount,
                VoucherDiscountAmount = b.VoucherDiscountAmount,
                FinalAmount = b.FinalAmount
            }).ToList();
        }

        public async Task<bool> CancelBookingAsync(int userId, int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);
            if (booking == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy lịch hẹn.");
            if (booking.Status != "Pending") throw new AutoWashPro.BLL.Exceptions.BadRequestException("Chỉ có thể hủy lịch ở trạng thái đang chờ (Pending).");

            bool isRefundable = (booking.ScheduledTime - DateTime.UtcNow).TotalHours >= 4;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                booking.Status = "Cancelled";

                var slot = await _context.TimeSlots.FirstOrDefaultAsync(s => s.BranchId == booking.BranchId && s.StartTime == booking.ScheduledTime.TimeOfDay);
                if (slot != null)
                {
                    var dailyCapacity = await _context.DailySlotCapacities
                        .FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.BranchId == booking.BranchId && dc.Date == booking.ScheduledTime.Date);

                    if (dailyCapacity != null && dailyCapacity.BookedWeight > 0)
                    {
                        dailyCapacity.BookedWeight -= booking.CapacityWeight;
                        if(dailyCapacity.BookedWeight < 0) dailyCapacity.BookedWeight = 0;
                    }
                }

                if (isRefundable)
                {
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                    if (wallet != null && booking.FinalAmount > 0 && await HasCompletedBookingPaymentAsync(booking.BookingId))
                    {
                        wallet.Balance += booking.FinalAmount;

                        var refundTx = new Transaction
                        {
                            WalletId = wallet.WalletId,
                            Amount = booking.FinalAmount,
                            TransactionType = "Refund",
                            Description = $"Hoàn tiền cọc do hủy lịch #{booking.BookingId}",
                            ReferenceBookingId = booking.BookingId
                        };
                        _context.Transactions.Add(refundTx);
                    }

                    if (booking.PointsUsed > 0)
                    {
                        await _walletService.RefundSpendablePointsAsync(
                            userId,
                            booking.PointsUsed,
                            $"{PointConstants.RefundPointsReasonPrefix} #{booking.BookingId}",
                            booking.BookingId);
                    }

                    if (booking.AppliedVoucherId.HasValue)
                    {
                        var userVoucher = await _context.UserVouchers
                            .Include(uv => uv.Voucher)
                            .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == booking.AppliedVoucherId.Value);
                        if (userVoucher != null)
                        {
                            if (userVoucher.UsageCount > 0)
                            {
                                userVoucher.UsageCount -= 1;
                            }

                            userVoucher.IsUsed = false;
                            userVoucher.UsedDate = userVoucher.UsageCount > 0 ? userVoucher.UsedDate : null;
                            userVoucher.LastUsedDate = userVoucher.UsageCount > 0 ? userVoucher.LastUsedDate : null;
                            if (userVoucher.Voucher.CurrentUsageCount > 0)
                            {
                                userVoucher.Voucher.CurrentUsageCount -= 1;
                            }
                        }
                    }
                }
                else
                {
                    // Late cancellation penalty
                    var userProfile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
                }

                booking.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Có lỗi xảy ra khi hủy lịch (xung đột dữ liệu). Vui lòng thử lại.");
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<(decimal voucherDiscount, decimal pointDiscount, int pointsUsed, decimal finalAmount, UserVoucher? userVoucher)>
            CalculateBookingPricingAsync(int userId, decimal originalPrice, int? voucherId, int pointsToUseRequest, DateTime scheduledTime)
        {
            decimal voucherDiscount = 0;
            UserVoucher? userVoucher = null;

            if (voucherId.HasValue)
            {
                userVoucher = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .FirstOrDefaultAsync(uv => uv.VoucherId == voucherId.Value && uv.UserId == userId);

                if (userVoucher == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Bạn không sở hữu Voucher này.");
                if (!userVoucher.Voucher.IsActive) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher này chưa được kích hoạt.");
                if (userVoucher.Voucher.StartDate.HasValue && userVoucher.Voucher.StartDate.Value > DateTime.UtcNow) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher này chưa đến thời gian áp dụng.");
                if (userVoucher.UsageCount >= userVoucher.Voucher.MaxUsagePerUser) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher này đã hết lượt sử dụng của bạn.");
                if (userVoucher.Voucher.MaxUsages > 0 && userVoucher.Voucher.CurrentUsageCount >= userVoucher.Voucher.MaxUsages) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher này đã hết tổng lượt sử dụng.");
                if (userVoucher.ExpiryDate < DateTime.UtcNow) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher này đã hết hạn.");
                if (userVoucher.Voucher.MinOrderAmount > 0 && originalPrice < userVoucher.Voucher.MinOrderAmount)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Voucher này chỉ áp dụng cho đơn từ {userVoucher.Voucher.MinOrderAmount:N0}đ.");

                if (userVoucher.Voucher.VoucherType == AutoWashPro.DAL.Enums.VoucherType.PhysicalGift)
                {
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher quà tặng hiện vật không thể dùng để trừ tiền hóa đơn.");
                }

                if (userVoucher.Voucher.ValidStartTime.HasValue && userVoucher.Voucher.ValidEndTime.HasValue)
                {
                    var timeOfDay = scheduledTime.ToVnTime().TimeOfDay;
                    var startTime = userVoucher.Voucher.ValidStartTime.Value;
                    var endTime = userVoucher.Voucher.ValidEndTime.Value;

                    bool isValidTime = false;
                    if (startTime <= endTime)
                    {
                        isValidTime = timeOfDay >= startTime && timeOfDay <= endTime;
                    }
                    else
                    {
                        isValidTime = timeOfDay >= startTime || timeOfDay <= endTime;
                    }

                    if (!isValidTime)
                    {
                        throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Voucher Happy Hour này chỉ áp dụng trong khung giờ từ {startTime:hh\\:mm} đến {endTime:hh\\:mm}.");
                    }
                }

                voucherDiscount = Math.Min(userVoucher.Voucher.DiscountAmount, originalPrice);
            }

            decimal remainingAfterVoucher = originalPrice - voucherDiscount;
            int pointsUsed = 0;
            decimal pointDiscount = 0;

            if (pointsToUseRequest > 0)
            {
                var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
                if (profile == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy hồ sơ khách hàng.");

                int maxPointsByBalance = profile.TotalPoint;
                int maxPointsByMoney = (int)(remainingAfterVoucher / PointConstants.VndPerSpendPoint);
                int pointsToApply = Math.Min(pointsToUseRequest, Math.Min(maxPointsByBalance, maxPointsByMoney));

                if (pointsToApply < pointsToUseRequest && pointsToApply == 0)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Không đủ điểm hoặc số tiền còn lại không cho phép dùng điểm.");

                pointsUsed = pointsToApply;
                pointDiscount = pointsUsed * PointConstants.VndPerSpendPoint;
            }

            decimal finalAmount = remainingAfterVoucher - pointDiscount;
            if (finalAmount < 0) finalAmount = 0;

            return (voucherDiscount, pointDiscount, pointsUsed, finalAmount, userVoucher);
        }

        public async Task<bool> UpdateVehicleConditionAsync(int staffId, int bookingId, UpdateVehicleConditionDTO request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingDetails)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId);

                if (booking == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy lịch hẹn.");
                if (booking.Status != "CheckedIn")
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Chỉ có thể cập nhật tình trạng xe khi xe đã Check-in tại trạm.");

                booking.VehicleCondition = request.Condition;

                decimal newSurcharge = 0;
                var basePrice = booking.BookingDetails.Sum(bd => bd.Price);

                if (request.Condition == VehicleCondition.Dirty)
                {
                    newSurcharge += basePrice * 0.2m; // 20% Upsell
                }
                else if (request.Condition == VehicleCondition.VeryDirty)
                {
                    newSurcharge += basePrice * 0.5m; // 50% Upsell
                }

                if (request.ActualVehicleTypeId.HasValue)
                {
                    booking.ActualVehicleTypeId = request.ActualVehicleTypeId.Value;
                    // In a real scenario, we might look up the price difference between booked VehicleType and ActualVehicleType.
                    // For now, we apply a flat mismatch surcharge (e.g., 30% of base price)
                    newSurcharge += basePrice * 0.3m;
                }

                decimal surchargeDiff = newSurcharge - booking.MismatchSurcharge;
                booking.MismatchSurcharge = newSurcharge;

                booking.OriginalPrice += surchargeDiff;
                booking.FinalAmount += surchargeDiff;

                if (surchargeDiff != 0 && booking.UserId.HasValue)
                {
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == booking.UserId.Value);
                    if (wallet != null)
                    {
                        if (surchargeDiff > 0)
                        {
                            if (wallet.Balance < surchargeDiff)
                            {
                                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Khách hàng không đủ số dư để thanh toán phụ phí. Cần thêm: {surchargeDiff:N0}đ");
                            }

                            wallet.Balance -= surchargeDiff;

                            var paymentTx = new Transaction
                            {
                                WalletId = wallet.WalletId,
                                Amount = -surchargeDiff,
                                TransactionType = "Payment",
                                Description = $"Thanh toán phụ phí do xe dơ cho lịch #{booking.BookingId}",
                                ReferenceBookingId = booking.BookingId
                            };
                            _context.Transactions.Add(paymentTx);
                        }
                        else if (surchargeDiff < 0)
                        {
                            // Refund for downgrading condition
                            decimal refundAmount = Math.Abs(surchargeDiff);
                            wallet.Balance += refundAmount;

                            var refundTx = new Transaction
                            {
                                WalletId = wallet.WalletId,
                                Amount = refundAmount,
                                TransactionType = "Refund",
                                Description = $"Hoàn tiền phụ phí do thay đổi tình trạng xe cho lịch #{booking.BookingId}",
                                ReferenceBookingId = booking.BookingId
                            };
                            _context.Transactions.Add(refundTx);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task MarkAsNoShowAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy Booking.");

            booking.Status = "NoShow";
            // GIỮ NGUYÊN TIỀN CỌC. TUYỆT ĐỐI KHÔNG GỌI HÀM HOÀN TIỀN (REFUND) Ở ĐÂY.

            await _context.SaveChangesAsync();
        }

        private async Task<decimal> GetPriceFromDb(int serviceId, int vehicleTypeId, VehicleCondition condition, int branchId)
        {
            var servicePrice = await _context.ServicePrices
                .FirstOrDefaultAsync(sp => sp.ServiceId == serviceId && sp.VehicleTypeId == vehicleTypeId && sp.BranchId == branchId);

            if (servicePrice == null) return 0;

            decimal price = servicePrice.Price;
            if (condition == VehicleCondition.VeryDirty)
                price *= 1.2m;

            return price;
        }

        public async Task ReportMismatchAsync(int bookingId, AutoWashPro.BLL.Enums.VehicleConditionEnum condition, int actualTypeId)
        {
            var booking = await _context.Bookings.Include(b => b.BookingDetails).FirstOrDefaultAsync(b => b.BookingId == bookingId);
            if (booking == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy Booking.");

            booking.VehicleCondition = (AutoWashPro.DAL.Entities.VehicleCondition)condition;
            booking.ActualVehicleTypeId = actualTypeId;

            decimal totalNewPrice = 0;
            decimal totalOldPrice = 0;
            foreach (var detail in booking.BookingDetails)
            {
                totalOldPrice += detail.Price;
                totalNewPrice += await GetPriceFromDb(detail.ServiceId, actualTypeId, (AutoWashPro.DAL.Entities.VehicleCondition)condition, booking.BranchId);
            }

            if (totalNewPrice > totalOldPrice)
            {
                booking.MismatchSurcharge = totalNewPrice - totalOldPrice;

                // Mock Push Notification
                Console.WriteLine($"[PUSH] Thông báo tới User: Phát sinh phụ phí {booking.MismatchSurcharge} VNĐ do sai lệch loại xe/độ bẩn.");
            }

            await _context.SaveChangesAsync();
        }

        public async Task<BookingResponseDTO> CreateWalkInBookingAsync(int staffId, CreateWalkInBookingDTO request)
        {
            if (request.ServiceIds == null || request.ServiceIds.Count == 0)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Vui lòng chọn ít nhất 1 dịch vụ.");

            int customerUserId = request.UserId;
            var targetDateTime = DateTime.UtcNow;

            // Anti-hoarding Rule
            bool hasActiveBooking = await _context.Bookings.AnyAsync(b => b.LicensePlate == request.LicensePlate && (b.Status == "Pending" || b.Status == "CheckedIn"));
            if (hasActiveBooking)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Xe biển số {request.LicensePlate} đang có lịch hẹn chưa hoàn thành.");

            var vehicleTypeQuery = await _context.Vehicles
                .Where(v => v.LicensePlate == request.LicensePlate && !v.IsDeleted)
                .Select(v => new { v.LicensePlate, v.VehicleType.BaseWeight, v.VehicleTypeId })
                .FirstOrDefaultAsync();

            if (vehicleTypeQuery == null)
            {
                 throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Xe với biển số {request.LicensePlate} không tồn tại trong hệ thống.");
            }

            var servicePrices = await _context.ServicePrices
                .Include(sp => sp.Service)
                .Where(sp => request.ServiceIds.Contains(sp.ServiceId) && sp.VehicleTypeId == vehicleTypeQuery.VehicleTypeId && sp.BranchId == request.BranchId)
                .ToListAsync();

            decimal totalOriginalPrice = 0;
            int maxCapacityWeight = 0;
            var pendingDetails = new List<BookingDetail>();

            foreach (var serviceId in request.ServiceIds)
            {
                var sp = servicePrices.FirstOrDefault(s => s.ServiceId == serviceId);
                if (sp == null)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Dịch vụ {serviceId} không hỗ trợ cho loại xe này.");

                var actualWeight = sp.CapacityWeight > 0 ? sp.CapacityWeight : vehicleTypeQuery.BaseWeight;
                if (actualWeight > maxCapacityWeight) maxCapacityWeight = actualWeight;

                totalOriginalPrice += sp.Price;

                pendingDetails.Add(new BookingDetail
                {
                    ServiceId = serviceId,
                    Price = sp.Price
                });
            }

            // Find current time slot to update capacity
            var timeOfDay = targetDateTime.TimeOfDay;
            var slot = await _context.TimeSlots
                .Where(s => s.BranchId == request.BranchId && s.StartTime <= timeOfDay && s.EndTime >= timeOfDay)
                .FirstOrDefaultAsync();

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

            if (slot != null)
            {
                var dailyCapacity = await _context.DailySlotCapacities.FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.BranchId == slot.BranchId && dc.Date == targetDateTime.Date);
                if (dailyCapacity == null)
                {
                    dailyCapacity = new DailySlotCapacity
                    {
                        SlotId = slot.SlotId,
                        BranchId = slot.BranchId,
                        Date = targetDateTime.Date,
                        BookedWeight = 0
                    };
                    _context.DailySlotCapacities.Add(dailyCapacity);
                    try { await _context.SaveChangesAsync(); }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
                    {
                        _context.Entry(dailyCapacity).State = EntityState.Detached;
                        dailyCapacity = await _context.DailySlotCapacities.FirstAsync(dc => dc.SlotId == slot.SlotId && dc.BranchId == slot.BranchId && dc.Date == targetDateTime.Date);
                    }
                }

                if (dailyCapacity.BookedWeight + maxCapacityWeight > slot.MaxCapacity)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Xưởng không đủ sức chứa cho xe này ngay lúc này. Vui lòng thử lại sau.");

                dailyCapacity.BookedWeight += maxCapacityWeight;
            }

            var (voucherDiscount, pointDiscount, pointsUsed, finalAmount, userVoucher) =
                await CalculateBookingPricingAsync(customerUserId, totalOriginalPrice, request.VoucherId, request.PointsToUse, targetDateTime);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == customerUserId);
            if (wallet == null || wallet.Balance < finalAmount)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Số dư ví của khách hàng không đủ để thanh toán. Cần: {finalAmount:N0}đ");

            try
            {
                if (slot != null)
                {
                    try { await _context.SaveChangesAsync(); }
                    catch (DbUpdateConcurrencyException) { throw new AutoWashPro.BLL.Exceptions.BadRequestException("Có người khác vừa đặt lịch. Vui lòng thử lại."); }
                }

                wallet.Balance -= finalAmount;
                var paymentTx = new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = -finalAmount,
                    TransactionType = "Payment",
                    Description = $"Thanh toán khách vãng lai lúc {targetDateTime:dd/MM/yyyy HH:mm}"
                };
                _context.Transactions.Add(paymentTx);

                if (userVoucher != null)
                {
                    userVoucher.UsageCount += 1;
                    userVoucher.IsUsed = userVoucher.UsageCount >= userVoucher.Voucher.MaxUsagePerUser;
                    userVoucher.UsedDate = DateTime.UtcNow;
                    userVoucher.LastUsedDate = DateTime.UtcNow;
                    userVoucher.Voucher.CurrentUsageCount += 1;
                }
                if (pointsUsed > 0)
                {
                    await _walletService.DeductSpendablePointsAsync(customerUserId, pointsUsed, "Dùng điểm giảm giá đặt lịch vãng lai");
                }

                var booking = new Booking
                {
                    UserId = customerUserId,
                    VehicleId = request.VehicleId,
                    LicensePlate = request.LicensePlate,
                    CapacityWeight = maxCapacityWeight,
                    VehicleCondition = VehicleCondition.Clean,
                    BranchId = request.BranchId,
                    ScheduledTime = targetDateTime,
                    Status = "CheckedIn", // Walk-ins go straight to CheckedIn
                    OriginalPrice = totalOriginalPrice,
                    PointsUsed = pointsUsed,
                    PointDiscountAmount = pointDiscount,
                    AppliedVoucherId = request.VoucherId,
                    VoucherDiscountAmount = voucherDiscount,
                    FinalAmount = finalAmount,
                    BookingDetails = pendingDetails,
                    FallbackQrCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                paymentTx.ReferenceBookingId = booking.BookingId;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                var serviceNames = await _context.Services.Where(s => request.ServiceIds.Contains(s.ServiceId)).Select(s => s.ServiceName).ToListAsync();

                return new BookingResponseDTO
                {
                    BookingId = booking.BookingId,
                    LicensePlate = booking.LicensePlate,
                    ServiceNames = serviceNames,
                    ScheduledTime = booking.ScheduledTime,
                    Status = booking.Status,
                    OriginalPrice = booking.OriginalPrice,
                    PointDiscountAmount = booking.PointDiscountAmount,
                    VoucherDiscountAmount = booking.VoucherDiscountAmount,
                    FinalAmount = booking.FinalAmount
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task ForceCancelBookingsAsync(ForceCancelRequestDTO request)
        {
            if (!request.TimeSlotId.HasValue && !request.AffectedDate.HasValue)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Vui lòng chọn ngày hoặc khung giờ để hủy lịch.");

            var query = _context.Bookings
                .Include(b => b.User)
                .Where(b => b.Status == "Pending" && b.BranchId == request.BranchId);

            if (request.AffectedDate.HasValue)
            {
                var targetDate = request.AffectedDate.Value.Date;
                query = query.Where(b => b.ScheduledTime.Date == targetDate);
            }

            if (request.TimeSlotId.HasValue)
            {
                var slot = await _context.TimeSlots.FindAsync(request.TimeSlotId.Value);
                if (slot != null)
                {
                    query = query.Where(b => b.ScheduledTime.TimeOfDay >= slot.StartTime && b.ScheduledTime.TimeOfDay <= slot.EndTime);
                }
            }

            var bookings = await query.ToListAsync();
            if (!bookings.Any()) return;

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
            try
            {
                foreach (var booking in bookings)
                {
                    booking.Status = "CancelledBySystem";

                    var pendingPaymentTransactions = await _context.Transactions
                        .Where(t => t.ReferenceBookingId == booking.BookingId
                                 && t.TransactionType == "BookingPayment"
                                 && t.Status == "Pending")
                        .ToListAsync();
                    foreach (var pendingPaymentTransaction in pendingPaymentTransactions)
                    {
                        pendingPaymentTransaction.Status = "Expired";
                    }

                    if (booking.UserId.HasValue)
                    {
                        var userId = booking.UserId.Value;

                        if (booking.FinalAmount > 0 && await HasCompletedBookingPaymentAsync(booking.BookingId)) { await _walletService.RefundBalanceAsync(userId, booking.FinalAmount, $"Hoàn tiền hủy lịch tự động: {request.Reason}"); }

                        if (booking.PointsUsed > 0)
                        {
                            await _walletService.RefundSpendablePointsAsync(userId, booking.PointsUsed, $"Hoàn điểm hủy lịch tự động: {request.Reason}", booking.BookingId);
                        }

                        await _voucherService.GenerateCompensationVoucherAsync(userId);

                        if (booking.User != null && !string.IsNullOrEmpty(booking.User.Email))
                        {
                            _ = Task.Run(() => _emailService.SendEmailAsync(
                                booking.User.Email,
                                "AutoWashPro - Thông báo hủy lịch do sự cố",
                                $"Kính chào quý khách,<br/><br/>Chúng tôi rất tiếc phải thông báo lịch hẹn của quý khách vào lúc {booking.ScheduledTime:dd/MM/yyyy HH:mm} đã bị hủy do sự cố bất khả kháng.<br/>Lý do: {request.Reason}<br/><br/>Chúng tôi đã hoàn lại toàn bộ số tiền <b>{booking.FinalAmount:N0}đ</b> và điểm tích lũy (nếu có) vào ví của quý khách.<br/>Đồng thời, để tạ lỗi, chúng tôi đã gửi tặng quý khách 1 Voucher giảm giá 30,000đ (có hạn 7 ngày) vào tài khoản.<br/><br/>Trân trọng,<br/>AutoWashPro"
                            ));
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private Task<bool> HasCompletedBookingPaymentAsync(int bookingId)
        {
            return _context.Transactions.AnyAsync(t =>
                t.ReferenceBookingId == bookingId
                && t.Status == "Completed"
                && (t.TransactionType == "Payment" || t.TransactionType == "BookingPayment"));
        }

    }
}
