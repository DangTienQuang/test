using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Constants;
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

        public BookingService(
            AutoWashDbContext context,
            IWalletService walletService,
            ITierService tierService,
            IEmailService emailService)
        {
            _context = context;
            _walletService = walletService;
            _tierService = tierService;
            _emailService = emailService;
        }

        public async Task<List<TimeSlotResponseDTO>> GetAvailableSlotsAsync(int userId, DateTime targetDate)
        {
            var userProfile = await _context.CustomerProfiles.Include(cp => cp.Tier).FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (userProfile == null || userProfile.Tier == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy thông tin hạng thành viên.");

            var maxDate = DateTime.UtcNow.Date.AddDays(userProfile.Tier.BookingWindowDays);
            if (targetDate.Date < DateTime.UtcNow.Date || targetDate.Date > maxDate)
            {
                throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Hạng {userProfile.Tier.TierName} chỉ được đặt trước từ hôm nay đến ngày {maxDate:dd/MM/yyyy}.");
            }

            var allSlots = await _context.TimeSlots.OrderBy(s => s.StartTime).ToListAsync();
            var response = new List<TimeSlotResponseDTO>();

            var existingBookings = await _context.Bookings
                .Where(b => b.ScheduledTime.Date == targetDate.Date && (b.Status == "Pending" || b.Status == "CheckedIn"))
                .ToListAsync();

            bool isVip = userProfile.Tier.TierName.ToLower() == "gold" || userProfile.Tier.TierName.ToLower() == "platinum";

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

                if (targetDate.Date == DateTime.UtcNow.Date && slot.StartTime < DateTime.UtcNow.TimeOfDay)
                {
                    slotDto.IsAvailable = false;
                    slotDto.Reason = "Đã qua giờ";
                }

                var bookedCount = existingBookings.Count(b => b.ScheduledTime.TimeOfDay == slot.StartTime);
                if (bookedCount >= slot.MaxCapacity)
                {
                    slotDto.IsAvailable = false;
                    slotDto.Reason = "Đã kín chỗ";
                }

                response.Add(slotDto);
            }

            return response;
        }
        public async Task<List<BookingResponseDTO>> GetAllBookingsByDateAsync(DateTime targetDate)
        {
            return await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.ScheduledTime.Date == targetDate.Date)
                .OrderBy(b => b.ScheduledTime)
                .Select(b => new BookingResponseDTO
                {
                    BookingId = b.BookingId,
                    LicensePlate = b.LicensePlate,
                    ServiceName = b.Service.ServiceName,
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status,
                    OriginalPrice = b.OriginalPrice,
                    PointDiscountAmount = b.PointDiscountAmount,
                    VoucherDiscountAmount = b.VoucherDiscountAmount,
                    FinalAmount = b.FinalAmount
                }).ToListAsync();
        }
        public async Task<BookingResponseDTO> GetBookingByIdAsync(int userId, int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);

            if (booking == null)
                throw new AutoWashPro.BLL.Exceptions.NotFoundException("Không tìm thấy chi tiết lịch hẹn hoặc bạn không có quyền xem.");

            return new BookingResponseDTO
            {
                BookingId = booking.BookingId,
                LicensePlate = booking.LicensePlate,
                ServiceName = booking.Service?.ServiceName ?? "N/A",
                ScheduledTime = booking.ScheduledTime,
                Status = booking.Status,
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

            var allowedStatuses = new[] { "Pending", "CheckedIn", "Completed", "Cancelled", "Delayed" };
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
                            await _tierService.EvaluateAndUpgradeTierAsync(booking.UserId.Value);
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
        public async Task<BookingResponseDTO> CreateBookingAsync(int userId, CreateBookingDTO request)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == request.LicensePlate && v.UserId == userId);
            if (vehicle == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Xe không tồn tại trong hồ sơ của bạn.");

            // Thêm Anti-hoarding Rule: Block việc đặt lịch mới nếu xe đã có lịch Pending/CheckedIn
            bool hasActiveBooking = await _context.Bookings.AnyAsync(b => b.LicensePlate == request.LicensePlate && (b.Status == "Pending" || b.Status == "CheckedIn"));
            if (hasActiveBooking) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Xe này đang có lịch hẹn chờ xử lý hoặc đã check-in. Không thể đặt thêm.");

            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null || !service.IsActive) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Dịch vụ không tồn tại hoặc đã ngừng kinh doanh.");

            var slot = await _context.TimeSlots.FindAsync(request.SlotId);
            if (slot == null) throw new AutoWashPro.BLL.Exceptions.NotFoundException("Khung giờ không hợp lệ.");

            var targetDateTime = request.ScheduledDate.Date.Add(slot.StartTime);

            var servicePrice = await _context.ServicePrices.FirstOrDefaultAsync(sp => sp.ServiceId == request.ServiceId && sp.VehicleTypeId == vehicle.VehicleTypeId);
            if (servicePrice == null) throw new AutoWashPro.BLL.Exceptions.BadRequestException("Dịch vụ này chưa hỗ trợ cho loại xe của bạn.");

            decimal originalPrice = servicePrice.Price;
            var (voucherDiscount, pointDiscount, pointsUsed, finalAmount, userVoucher) =
                await CalculateBookingPricingAsync(userId, originalPrice, request.VoucherId, request.PointsToUse);

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null || wallet.Balance < finalAmount) throw new AutoWashPro.BLL.Exceptions.BadRequestException($"Số dư ví không đủ để đặt cọc. Cần: {finalAmount:N0}đ");
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var dailyCapacity = await _context.DailySlotCapacities
                    .FirstOrDefaultAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);

                if (dailyCapacity == null)
                {
                    dailyCapacity = new DailySlotCapacity
                    {
                        SlotId = slot.SlotId,
                        Date = targetDateTime.Date,
                        BookedCount = 0
                    };
                    _context.DailySlotCapacities.Add(dailyCapacity);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry") == true)
                    {
                         // Nếu ai đó đã thêm, lấy lại capacity vừa được tạo
                        _context.Entry(dailyCapacity).State = EntityState.Detached;
                        dailyCapacity = await _context.DailySlotCapacities
                            .FirstAsync(dc => dc.SlotId == slot.SlotId && dc.Date == targetDateTime.Date);
                    }
                }

                if (dailyCapacity.BookedCount >= slot.MaxCapacity)
                {
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Khung giờ này vừa mới hết chỗ, vui lòng chọn giờ khác.");
                }

                dailyCapacity.BookedCount++;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new AutoWashPro.BLL.Exceptions.BadRequestException("Có người khác vừa đặt lịch. Vui lòng thử lại.");
                }

                wallet.Balance -= finalAmount;

                var paymentTx = new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = -finalAmount,
                    TransactionType = "Payment",
                    Description = $"Thanh toán cọc lịch rửa xe {request.LicensePlate} lúc {targetDateTime:dd/MM/yyyy HH:mm}"
                };
                _context.Transactions.Add(paymentTx);

                if (userVoucher != null)
                {
                    userVoucher.IsUsed = true;
                    userVoucher.UsedDate = DateTime.UtcNow;
                }

                if (pointsUsed > 0)
                {
                    await _walletService.DeductSpendablePointsAsync(
                        userId, pointsUsed, $"Dùng điểm giảm giá đặt lịch {request.LicensePlate}");
                }

                var booking = new Booking
                {
                    UserId = userId,
                    LicensePlate = request.LicensePlate,
                    ServiceId = request.ServiceId,
                    ScheduledTime = targetDateTime,
                    Status = "Pending",
                    OriginalPrice = originalPrice,
                    PointsUsed = pointsUsed,
                    PointDiscountAmount = pointDiscount,
                    AppliedVoucherId = request.VoucherId,
                    VoucherDiscountAmount = voucherDiscount,
                    FinalAmount = finalAmount
                };
                _context.Bookings.Add(booking);

                await _context.SaveChangesAsync();

                paymentTx.ReferenceBookingId = booking.BookingId;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                try
                {
                    var user = await _context.Users
                        .Include(u => u.CustomerProfile)
                        .FirstOrDefaultAsync(u => u.UserId == userId);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        string subject = $"[SmartWash] Đặt lịch thành công - #{booking.BookingId}";
                        string customerName = user.CustomerProfile?.FullName ?? "Quý khách";

                        string htmlMessage = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 10px;'>
                            <h2 style='color: #007bff; text-align: center;'>SMARTWASH XÁC NHẬN ĐẶT LỊCH</h2>
                            <p>Xin chào <b>{customerName}</b>,</p>
                            <p>Cảm ơn bạn đã sử dụng dịch vụ của SmartWash. Dưới đây là thông tin lịch hẹn của bạn:</p>
                            
                            <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
                                <tr>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Mã lịch hẹn:</b></td>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'>#{booking.BookingId}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Biển số xe:</b></td>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'>{booking.LicensePlate}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Dịch vụ:</b></td>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'>{service.ServiceName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Thời gian:</b></td>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee; color: red;'><b>{targetDateTime:dd/MM/yyyy HH:mm}</b></td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee;'><b>Đã thanh toán (Cọc):</b></td>
                                    <td style='padding: 8px; border-bottom: 1px solid #eee; color: green;'><b>{finalAmount:N0} đ</b></td>
                                </tr>
                            </table>

                            <p style='margin-top: 20px;'>Vui lòng đến trạm đúng giờ. Nếu bạn đến trễ quá 15 phút, hệ thống sẽ tự động hủy lịch (hoặc xếp vào hàng chờ).</p>
                            <p>Trân trọng,<br><b>Đội ngũ SmartWash</b></p>
                        </div>";

                        await _emailService.SendEmailAsync(user.Email, subject, htmlMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Lỗi gửi mail]: {ex.Message}");
                }
                return new BookingResponseDTO
                {
                    BookingId = booking.BookingId,
                    LicensePlate = booking.LicensePlate,
                    ServiceName = service.ServiceName,
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
            return await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.ScheduledTime)
                .Select(b => new BookingResponseDTO
                {
                    BookingId = b.BookingId,
                    LicensePlate = b.LicensePlate,
                    ServiceName = b.Service.ServiceName,
                    ScheduledTime = b.ScheduledTime,
                    Status = b.Status,
                    OriginalPrice = b.OriginalPrice,
                    PointDiscountAmount = b.PointDiscountAmount,
                    FinalAmount = b.FinalAmount
                }).ToListAsync();
        }

        public async Task<bool> CancelBookingAsync(int userId, int bookingId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId);
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

                    if (dailyCapacity != null && dailyCapacity.BookedCount > 0)
                    {
                        dailyCapacity.BookedCount--;

                        try
                        {
                            await _context.SaveChangesAsync();
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            throw new AutoWashPro.BLL.Exceptions.BadRequestException("Có lỗi xảy ra khi hủy lịch. Vui lòng thử lại.");
                        }
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

                booking.UpdatedAt = DateTime.UtcNow;
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
    }
}
