using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.Helpers;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
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

        public BookingService(
            AutoWashDbContext context,
            IWalletService walletService,
            ITierService tierService,
            IEmailService emailService, IVoucherService voucherService)
        {
            _context = context;
            _walletService = walletService;
            _tierService = tierService;
            _emailService = emailService;
            _voucherService = voucherService;
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
            if (request.BookingVehicles != null && request.BookingVehicles.Any())
            {
                foreach (var item in request.BookingVehicles)
                {
                    var servicePrice = await _context.ServicePrices
                        .FirstOrDefaultAsync(sp => sp.ServiceId == item.ServiceId
                                                && sp.VehicleTypeId == item.VehicleTypeId);

                    if (servicePrice != null)
                    {
                        if (servicePrice.CapacityWeight > 0)
                        {
                            totalRequestWeight += servicePrice.CapacityWeight;
                        }
                        else
                        {
                            var vehicleType = await _context.VehicleTypes.FirstOrDefaultAsync(vt => vt.Id == item.VehicleTypeId);
                            if (vehicleType != null)
                            {
                                totalRequestWeight += vehicleType.BaseWeight;
                            }
                        }
                    }
                }
            }

            var allSlots = await _context.TimeSlots.OrderBy(s => s.StartTime).ToListAsync();
            var response = new List<TimeSlotResponseDTO>();

            var dailyCapacities = await _context.DailySlotCapacities
                .Where(dc => dc.Date == request.TargetDate.Date)
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
                LicensePlate = string.Join(", ", b.BookingDetails.Select(d => d.LicensePlate)),
                ServiceName = string.Join(", ", b.BookingDetails.Select(d => d.Service.ServiceName)),
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
                LicensePlate = string.Join(", ", booking.BookingDetails.Select(d => d.LicensePlate)),
                ServiceName = string.Join(", ", booking.BookingDetails.Select(d => d.Service.ServiceName)),
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

        public async Task<BookingResponseDTO> UpdateBookingStatusByLicensePlateAsync(string licensePlate, string newStatus)
        {
            var allowedStatuses = new[] { "Pending", "CheckedIn", "Completed", "Cancelled", "Delayed", "CancelledBySystem" };
            if (!allowedStatuses.Contains(newStatus)) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Trạng thái không hợp lệ.");

            var normalizedPlate = NormalizeLicensePlate(licensePlate);
            if (string.IsNullOrEmpty(normalizedPlate))
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Biển số xe không hợp lệ.");

            var todayInVN = DateTime.UtcNow.ToVnTime().Date;

            // Tìm Booking có chứa xe này TRONG NGÀY HÔM NAY và ở trạng thái hợp lý để update.
            // Ví dụ: Muốn CheckedIn -> tìm booking Pending
            // Muốn Completed -> tìm booking CheckedIn
            // Khắc phục timezone bug: scheduledTime được lưu ở database (thường là UTC).
            // Ta tính khoảng đầu ngày và cuối ngày của VN, rồi chuyển ngược về UTC để query an toàn.
            // Nhưng vì database có thể đang lưu ScheduledTime là ngày giờ VN (do hàm createBooking),
            // Ta sẽ query một khoảng thời gian rộng hơn và lọc lại ở in-memory hoặc query cẩn thận.
            // Do dữ liệu license plate có thể lưu dưới dạng chuẩn hoặc có gạch ngang,
            // dùng db function hoặc as enumerable để filter chính xác. Để tối ưu ta lấy lên trước.
            //
            // Cập nhật: hệ thống luôn lưu LicensePlate với các ký tự, nhưng khi lookup ta cần Normalize
            // cả record trong db để so khớp chính xác nhất.
            var startOfDayInVN = todayInVN.Date;
            var endOfDayInVN = startOfDayInVN.AddDays(1).AddTicks(-1);

            // Do trong DB ScheduledTime có thể được lưu bằng UTC, ta convert ngày giờ VN sang UTC để query
            // lấy một khoảng đủ rộng (UTC time) để chắc chắn bao gồm cả ngày theo giờ VN.
            var startOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(startOfDayInVN, TimeZoneInfo.FindSystemTimeZoneById(
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh"));
            var endOfDayUtc = TimeZoneInfo.ConvertTimeToUtc(endOfDayInVN, TimeZoneInfo.FindSystemTimeZoneById(
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh"));

            var query = _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Service)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.ActualVehicleType)
                .Include(b => b.User)
                    .ThenInclude(u => u.CustomerProfile)
                .Where(b => b.ScheduledTime >= startOfDayUtc && b.ScheduledTime <= endOfDayUtc);

            if (newStatus == "CheckedIn")
            {
                query = query.Where(b => b.Status == "Pending");
            }
            else if (newStatus == "Completed")
            {
                query = query.Where(b => b.Status == "CheckedIn");
            }
            else
            {
                // Cho phép huỷ nếu chưa hoàn thành
                query = query.Where(b => b.Status == "Pending" || b.Status == "CheckedIn");
            }

            var bookings = await query.ToListAsync();

            // Tìm in-memory để loại bỏ ký tự đặc biệt khi so sánh biển số xe
            var matchedBookings = bookings.Where(b =>
                b.BookingDetails.Any(bd => NormalizeLicensePlate(bd.LicensePlate) == normalizedPlate)).ToList();

            if (matchedBookings.Count == 0)
            {
                throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Không tìm thấy lịch hẹn hợp lệ trong ngày hôm nay cho xe có biển số {licensePlate}. Vui lòng kiểm tra lại trạng thái hiện tại của xe.");
            }

            if (matchedBookings.Count > 1)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Phát hiện nhiều lịch hẹn ({matchedBookings.Count}) hợp lệ cho xe có biển số {licensePlate} trong ngày hôm nay. Vui lòng sử dụng mã Booking ID để cập nhật chính xác.");
            }

            var booking = matchedBookings.First();

            // Gọi lại hàm UpdateBookingStatusAsync để tái sử dụng logic thưởng điểm
            var isUpdated = await UpdateBookingStatusAsync(booking.BookingId, newStatus);

            if (!isUpdated)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Cập nhật trạng thái thất bại.");
            }

            // Map ra DTO để trả về cho nhân viên xác nhận thông tin
            return new BookingResponseDTO
            {
                BookingId = booking.BookingId,
                LicensePlate = normalizedPlate,
                ServiceName = string.Join(", ", booking.BookingDetails.Select(d => d.Service.ServiceName)),
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
            return true;
        }

        public async Task<CompatibilityDTO> ValidateBookingCompatibilityAsync(int userId, int slotId, DateTime targetDate, List<VehicleBookingItemDTO> vehicles)
        {
            if (vehicles == null || vehicles.Count == 0)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Giỏ hàng không có xe nào.");

            if (vehicles.Count > 5)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Tài khoản cá nhân chỉ được phép đặt tối đa 5 xe trong một lần đặt lịch.");

            var duplicatePlates = vehicles.GroupBy(v => v.LicensePlate).Where(g => g.Count() > 1).Any();
            if (duplicatePlates)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Danh sách xe có chứa biển số bị trùng lặp.");

            var slot = await _context.TimeSlots.FindAsync(slotId);
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

            foreach (var item in vehicles)
            {
                var vehicle = await _context.Vehicles.Include(v => v.VehicleType).FirstOrDefaultAsync(v => v.LicensePlate == item.LicensePlate && v.UserId == userId && !v.IsDeleted);
                if (vehicle == null)
                    throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Xe với biển số {item.LicensePlate} không tồn tại trong hồ sơ của bạn.");

                bool hasActiveBooking = await _context.Bookings.AnyAsync(b => b.BookingDetails.Any(bd => bd.LicensePlate == item.LicensePlate) && (b.Status == "Pending" || b.Status == "CheckedIn"));
                if (hasActiveBooking)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Xe biển số {item.LicensePlate} đang có lịch hẹn chưa hoàn thành. Không thể đặt thêm lịch mới cho xe này.");

                var service = await _context.Services.FindAsync(item.ServiceId);
                if (service == null || !service.IsActive)
                    throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Dịch vụ cho xe {item.LicensePlate} không tồn tại hoặc đã ngừng kinh doanh.");

                var servicePrice = await _context.ServicePrices.FirstOrDefaultAsync(sp => sp.ServiceId == item.ServiceId && sp.VehicleTypeId == vehicle.VehicleTypeId);
                if (servicePrice == null)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Dịch vụ này chưa hỗ trợ cho loại xe {item.LicensePlate}.");

                var actualCapacityWeight = servicePrice.CapacityWeight > 0 ? servicePrice.CapacityWeight : vehicle.VehicleType.BaseWeight;
                totalCapacityWeight += actualCapacityWeight;
            }

            var dailyCapacity = await _context.DailySlotCapacities.FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
            int bookedWeight = dailyCapacity?.BookedWeight ?? 0;
            int remainingCapacity = slot.MaxCapacity - bookedWeight;

            if (bookedWeight + totalCapacityWeight > slot.MaxCapacity)
            {
                return new CompatibilityDTO
                {
                    IsCompatible = false,
                    Message = "Xưởng không đủ sức chứa cho giỏ hàng của bạn.",
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

        public async Task<CompatibilityDTO> CheckCompatibilityAsync(int userId, CheckCompatibilityRequestDTO request)
        {
            return await ValidateBookingCompatibilityAsync(userId, request.SlotId, request.TargetDate, request.Vehicles);
        }

        public async Task<BookingResponseDTO> CreateBookingAsync(int userId, CreateBookingDTO request)
        {
            var compatibility = await ValidateBookingCompatibilityAsync(userId, request.SlotId, request.ScheduledDate, request.Vehicles.Select(v => new VehicleBookingItemDTO { LicensePlate = v.LicensePlate, ServiceId = v.ServiceId }).ToList());

            if (!compatibility.IsCompatible)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException(compatibility.Message ?? "Xưởng không đủ sức chứa cho giỏ hàng của bạn.");
            }

            var slot = await _context.TimeSlots.FindAsync(request.SlotId);
            var targetDateTime = request.ScheduledDate.Date.Add(slot.StartTime);

            decimal totalOriginalPrice = 0;
            int totalCapacityWeight = compatibility.TotalCapacityWeight;
            var pendingDetails = new List<BookingDetail>();

            foreach (var item in request.Vehicles)
            {
                var vehicle = await _context.Vehicles.Include(v => v.VehicleType).FirstOrDefaultAsync(v => v.LicensePlate == item.LicensePlate && v.UserId == userId && !v.IsDeleted);
                var servicePrice = await _context.ServicePrices.FirstOrDefaultAsync(sp => sp.ServiceId == item.ServiceId && sp.VehicleTypeId == vehicle.VehicleTypeId);

                var actualCapacityWeight = servicePrice.CapacityWeight > 0 ? servicePrice.CapacityWeight : vehicle.VehicleType.BaseWeight;

                totalOriginalPrice += servicePrice.Price;

                pendingDetails.Add(new BookingDetail
                {
                    LicensePlate = item.LicensePlate,
                    ServiceId = item.ServiceId,
                    Price = servicePrice.Price,
                    CapacityWeight = actualCapacityWeight,
                    VehicleCondition = VehicleCondition.Clean
                });
            }

            // PHASE 3: Capacity AI
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var dailyCapacity = await _context.DailySlotCapacities.FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
            if (dailyCapacity == null)
            {
                dailyCapacity = new DailySlotCapacity
                {
                    SlotId = slot.SlotId,
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
                    dailyCapacity = await _context.DailySlotCapacities.FirstAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
                }
            }
            else
            {
               // Re-fetch to ensure we have the latest version before modifying
               dailyCapacity = await _context.DailySlotCapacities.FirstAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
            }

            if (dailyCapacity.BookedWeight + totalCapacityWeight > slot.MaxCapacity)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Xưởng không đủ sức chứa cho số lượng xe này. Vui lòng giảm bớt xe hoặc chọn khung giờ khác.");

            // PHASE 4: Financial Math
            var (voucherDiscount, pointDiscount, pointsUsed, finalAmount, userVoucher) =
                await CalculateBookingPricingAsync(userId, totalOriginalPrice, request.VoucherId, request.PointsToUse);

            // PHASE 5: Transaction
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null || wallet.Balance < finalAmount)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Số dư ví không đủ để đặt cọc. Cần: {finalAmount:N0}đ");

            try
            {
                // Update Capacity
                dailyCapacity.BookedWeight += totalCapacityWeight;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Có người khác vừa đặt lịch. Vui lòng thử lại.");
                }

                // Deduct Wallet
                wallet.Balance -= finalAmount;

                var paymentTx = new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = -finalAmount,
                    TransactionType = "Payment",
                    Description = $"Thanh toán cọc lịch rửa xe lúc {targetDateTime:dd/MM/yyyy HH:mm}"
                };
                _context.Transactions.Add(paymentTx);

                // Apply Voucher & Points
                if (userVoucher != null)
                {
                    userVoucher.IsUsed = true;
                    userVoucher.UsedDate = DateTime.UtcNow;
                }

                if (pointsUsed > 0)
                {
                    await _walletService.DeductSpendablePointsAsync(userId, pointsUsed, "Dùng điểm giảm giá đặt lịch");
                }

                // Create Booking
                var booking = new Booking
                {
                    UserId = userId,
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

                paymentTx.ReferenceBookingId = booking.BookingId;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // PHASE 6: Post-Processing
                try
                {
                    var user = await _context.Users.Include(u => u.CustomerProfile).FirstOrDefaultAsync(u => u.UserId == userId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailHtml = BLL.Helpers.EmailTemplateBuilder.BuildBookingConfirmationEmail(booking, pendingDetails, user.CustomerProfile?.FullName ?? "Quý khách");
                        await _emailService.SendEmailAsync(user.Email, $"[SmartWash] Đặt lịch thành công - #{booking.BookingId}", emailHtml);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Lỗi gửi mail]: {ex.Message}");
                }

                return new BookingResponseDTO
                {
                    BookingId = booking.BookingId,
                    LicensePlate = string.Join(", ", pendingDetails.Select(d => d.LicensePlate)),
                    ServiceName = "Nhiều dịch vụ",
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
                LicensePlate = string.Join(", ", b.BookingDetails.Select(d => d.LicensePlate)),
                ServiceName = string.Join(", ", b.BookingDetails.Select(d => d.Service.ServiceName)),
                ScheduledTime = b.ScheduledTime,
                Status = b.Status,
                OriginalPrice = b.OriginalPrice,
                PointDiscountAmount = b.PointDiscountAmount,
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

                var slot = await _context.TimeSlots.FirstOrDefaultAsync(s => s.StartTime == booking.ScheduledTime.TimeOfDay);
                if (slot != null)
                {
                    var dailyCapacity = await _context.DailySlotCapacities
                        .FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.Date == booking.ScheduledTime.Date);

                    if (dailyCapacity != null && dailyCapacity.BookedWeight > 0)
                    {
                        int weightToSubtract = await _context.BookingDetails
                             .Where(bd => bd.BookingId == bookingId)
                             .SumAsync(bd => bd.CapacityWeight);
                       

                        dailyCapacity.BookedWeight -= weightToSubtract;
                        if(dailyCapacity.BookedWeight < 0) dailyCapacity.BookedWeight = 0;
                    }
                }

                if (isRefundable)
                {
                    var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                    if (wallet != null && booking.FinalAmount > 0)
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
                            .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == booking.AppliedVoucherId.Value);
                        if (userVoucher != null)
                        {
                            userVoucher.IsUsed = false;
                            userVoucher.UsedDate = null;
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
            CalculateBookingPricingAsync(int userId, decimal originalPrice, int? voucherId, int pointsToUseRequest)
        {
            decimal voucherDiscount = 0;
            UserVoucher? userVoucher = null;

            if (voucherId.HasValue)
            {
                userVoucher = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .FirstOrDefaultAsync(uv => uv.VoucherId == voucherId.Value && uv.UserId == userId);

                if (userVoucher == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Bạn không sở hữu Voucher này.");
                if (userVoucher.IsUsed) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher này đã được sử dụng.");
                if (userVoucher.Voucher.ExpiryDate < DateTime.UtcNow) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Voucher này đã hết hạn.");

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

                var detail = booking.BookingDetails.FirstOrDefault(d => d.DetailId == request.DetailId);
                if (detail == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy thông tin xe trong lịch hẹn này.");

                detail.VehicleCondition = request.Condition;

                decimal newSurcharge = 0;

                if (request.Condition == VehicleCondition.Dirty)
                {
                    newSurcharge += detail.Price * 0.2m; // 20% Upsell
                }
                else if (request.Condition == VehicleCondition.VeryDirty)
                {
                    newSurcharge += detail.Price * 0.5m; // 50% Upsell
                }

                if (request.ActualVehicleTypeId.HasValue)
                {
                    detail.ActualVehicleTypeId = request.ActualVehicleTypeId.Value;
                    // In a real scenario, we might look up the price difference between booked VehicleType and ActualVehicleType.
                    // For now, we apply a flat mismatch surcharge (e.g., 30% of base price)
                    newSurcharge += detail.Price * 0.3m;
                }

                decimal surchargeDiff = newSurcharge - detail.MismatchSurcharge;
                detail.MismatchSurcharge = newSurcharge;

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

        private async Task<decimal> GetPriceFromDb(int serviceId, int vehicleTypeId, VehicleCondition condition)
        {
            var servicePrice = await _context.ServicePrices
                .FirstOrDefaultAsync(sp => sp.ServiceId == serviceId && sp.VehicleTypeId == vehicleTypeId);

            if (servicePrice == null) return 0;

            decimal price = servicePrice.Price;
            if (condition == VehicleCondition.VeryDirty)
                price *= 1.2m;

            return price;
        }

        public async Task ReportMismatchAsync(int detailId, VehicleCondition condition, int actualTypeId)
        {
            var detail = await _context.BookingDetails.FindAsync(detailId);
            if (detail == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy chi tiết xe.");

            detail.VehicleCondition = condition;
            detail.ActualVehicleTypeId = actualTypeId;

            decimal newPrice = await GetPriceFromDb(detail.ServiceId, actualTypeId, condition);

            if (newPrice > detail.Price)
            {
                detail.MismatchSurcharge = newPrice - detail.Price;

                // Mock Push Notification
                Console.WriteLine($"[PUSH] Thông báo tới User: Phát sinh phụ phí {detail.MismatchSurcharge} VNĐ do sai lệch loại xe/độ bẩn.");
            }

            await _context.SaveChangesAsync();
        }

        public async Task<BookingResponseDTO> CreateWalkInBookingAsync(int staffId, CreateWalkInBookingDTO request)
        {
            if (request.Vehicles == null || request.Vehicles.Count == 0)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Giỏ hàng không có xe nào.");

            if (request.Vehicles.Count > 5)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Tài khoản cá nhân chỉ được phép đặt tối đa 5 xe trong một lần đặt lịch.");

            int customerUserId = request.UserId;
            // Similar validation but skips specific time slot and uses immediate current time
            var duplicatePlates = request.Vehicles.GroupBy(v => v.LicensePlate).Where(g => g.Count() > 1).Any();
            if (duplicatePlates)
                throw new AutoWashPro.BLL.Exceptions.BadRequestException("Danh sách xe có chứa biển số bị trùng lặp.");

            var targetDateTime = DateTime.UtcNow;

            decimal totalOriginalPrice = 0;
            int totalCapacityWeight = 0;
            var pendingDetails = new List<BookingDetail>();

            foreach (var item in request.Vehicles)
            {
                var vehicle = await _context.Vehicles.Include(v => v.VehicleType).FirstOrDefaultAsync(v => v.LicensePlate == item.LicensePlate && !v.IsDeleted);
                if (vehicle == null)
                    throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Xe với biển số {item.LicensePlate} không tồn tại trong hệ thống.");

                // Anti-hoarding Rule
                bool hasActiveBooking = await _context.Bookings.AnyAsync(b => b.BookingDetails.Any(bd => bd.LicensePlate == item.LicensePlate) && (b.Status == "Pending" || b.Status == "CheckedIn"));
                if (hasActiveBooking)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Xe biển số {item.LicensePlate} đang có lịch hẹn chưa hoàn thành.");

                var service = await _context.Services.FindAsync(item.ServiceId);
                if (service == null || !service.IsActive)
                    throw new AutoWashPro.BLL.Exceptions.NotFoundException($"Dịch vụ cho xe {item.LicensePlate} không tồn tại hoặc đã ngừng kinh doanh.");

                var servicePrice = await _context.ServicePrices.FirstOrDefaultAsync(sp => sp.ServiceId == item.ServiceId && sp.VehicleTypeId == vehicle.VehicleTypeId);
                if (servicePrice == null)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Dịch vụ này chưa hỗ trợ cho loại xe {item.LicensePlate}.");

                var actualCapacityWeight = servicePrice.CapacityWeight > 0 ? servicePrice.CapacityWeight : vehicle.VehicleType.BaseWeight;

                totalOriginalPrice += servicePrice.Price;
                totalCapacityWeight += actualCapacityWeight;

                pendingDetails.Add(new BookingDetail
                {
                    LicensePlate = item.LicensePlate,
                    ServiceId = item.ServiceId,
                    Price = servicePrice.Price,
                    CapacityWeight = actualCapacityWeight,
                    VehicleCondition = VehicleCondition.Clean
                });
            }

            // Find current time slot to update capacity
            var timeOfDay = targetDateTime.TimeOfDay;
            var slot = await _context.TimeSlots
                .Where(s => s.StartTime <= timeOfDay && s.EndTime >= timeOfDay)
                .FirstOrDefaultAsync();

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            if (slot != null)
            {
                var dailyCapacity = await _context.DailySlotCapacities.FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
                if (dailyCapacity == null)
                {
                    dailyCapacity = new DailySlotCapacity
                    {
                        SlotId = slot.SlotId,
                        Date = targetDateTime.Date,
                        BookedWeight = 0
                    };
                    _context.DailySlotCapacities.Add(dailyCapacity);
                    try { await _context.SaveChangesAsync(); }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
                    {
                        _context.Entry(dailyCapacity).State = EntityState.Detached;
                        dailyCapacity = await _context.DailySlotCapacities.FirstAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
                    }
                }

                if (dailyCapacity.BookedWeight + totalCapacityWeight > slot.MaxCapacity)
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Xưởng không đủ sức chứa cho số lượng xe này ngay lúc này. Vui lòng thử lại sau.");

                dailyCapacity.BookedWeight += totalCapacityWeight;
            }

            var (voucherDiscount, pointDiscount, pointsUsed, finalAmount, userVoucher) =
                await CalculateBookingPricingAsync(customerUserId, totalOriginalPrice, request.VoucherId, request.PointsToUse);

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
                    userVoucher.IsUsed = true;
                    userVoucher.UsedDate = DateTime.UtcNow;
                }
                if (pointsUsed > 0)
                {
                    await _walletService.DeductSpendablePointsAsync(customerUserId, pointsUsed, "Dùng điểm giảm giá đặt lịch vãng lai");
                }

                var booking = new Booking
                {
                    UserId = customerUserId,
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

                return new BookingResponseDTO
                {
                    BookingId = booking.BookingId,
                    LicensePlate = string.Join(", ", pendingDetails.Select(d => d.LicensePlate)),
                    ServiceName = "Nhiều dịch vụ (Walk-in)",
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
                .Where(b => b.Status == "Pending");

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

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                foreach (var booking in bookings)
                {
                    booking.Status = "CancelledBySystem";

                    if (booking.UserId.HasValue)
                    {
                        var userId = booking.UserId.Value;

                        if (booking.FinalAmount > 0) { await _walletService.RefundBalanceAsync(userId, booking.FinalAmount, $"Hoàn tiền hủy lịch tự động: {request.Reason}"); }

                        if (booking.PointsUsed > 0)
                        {
                            await _walletService.RefundSpendablePointsAsync(userId, booking.PointsUsed, $"Hoàn điểm hủy lịch tự động: {request.Reason}", booking.BookingId);
                        }

                        await _voucherService.GenerateCompensationVoucherAsync(userId);

                        if (booking.User != null && !string.IsNullOrEmpty(booking.User.Email))
                        {
                            _ = _emailService.SendEmailAsync(
                                booking.User.Email,
                                "AutoWashPro - Thông báo hủy lịch do sự cố",
                                $"Kính chào quý khách,<br/><br/>Chúng tôi rất tiếc phải thông báo lịch hẹn của quý khách vào lúc {booking.ScheduledTime:dd/MM/yyyy HH:mm} đã bị hủy do sự cố bất khả kháng.<br/>Lý do: {request.Reason}<br/><br/>Chúng tôi đã hoàn lại toàn bộ số tiền <b>{booking.FinalAmount:N0}đ</b> và điểm tích lũy (nếu có) vào ví của quý khách.<br/>Đồng thời, để tạ lỗi, chúng tôi đã gửi tặng quý khách 1 Voucher giảm giá 30,000đ (có hạn 7 ngày) vào tài khoản.<br/><br/>Trân trọng,<br/>AutoWashPro"
                            );
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
    }
}
