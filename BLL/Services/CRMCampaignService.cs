using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using AutoWashPro.BLL.Helpers;

namespace AutoWashPro.BLL.Services
{
    public class CRMCampaignService : ICRMCampaignService
    {
        private readonly AutoWashDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<CRMCampaignService> _logger;

        public CRMCampaignService(AutoWashDbContext context, IEmailService emailService, ILogger<CRMCampaignService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task RunBirthdayCampaignAsync()
        {
            _logger.LogInformation("Bắt đầu quét khách hàng có sinh nhật hôm nay...");

            var today = DateTime.UtcNow.ToVnTime().Date;

            var birthdayUsers = await _context.Users
                .Include(u => u.CustomerProfile)
                .Where(u => u.CustomerProfile != null
                            && u.CustomerProfile.DateOfBirth.HasValue
                            && u.CustomerProfile.DateOfBirth.Value.Month == today.Month
                            && u.CustomerProfile.DateOfBirth.Value.Day == today.Day
                            && (u.CustomerProfile.LastBirthdayGiftYear == null || u.CustomerProfile.LastBirthdayGiftYear < today.Year)
                            && u.Status == "Active")
                .ToListAsync();

            if (!birthdayUsers.Any())
            {
                _logger.LogInformation("Không có khách hàng nào hợp lệ nhận quà sinh nhật hôm nay.");
                return;
            }

            foreach (var user in birthdayUsers)
            {
                var code = $"HPBD-{user.UserId}-{today.Year}";

                var hasVoucher = await _context.Vouchers.AnyAsync(v => v.Code == code);
                if (hasVoucher) continue;

                var voucher = new Voucher
                {
                    Code = code,
                    DiscountAmount = 50000,
                    MaxUsages = 1,
                    PointsRequired = 0,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    VoucherType = AutoWashPro.DAL.Enums.VoucherType.Discount
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                var userVoucher = new UserVoucher
                {
                    UserId = user.UserId,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);

                user.CustomerProfile.LastBirthdayGiftYear = today.Year;

                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogInformation($"Gửi email chúc mừng sinh nhật cho User {user.UserId} ({user.Email})");
                    var emailHtml = $@"
                        <h3>Chúc mừng sinh nhật, {user.CustomerProfile.FullName}!</h3>
                        <p>AutoWashPro gửi tặng bạn một phần quà sinh nhật:</p>
                        <p><b>Voucher Giảm 50,000đ</b> (Mã: {code}) đã được thêm vào ví của bạn.</p>
                        <p>Hạn sử dụng: 7 ngày kể từ hôm nay.</p>
                        <p>Hãy đặt lịch và mang xế cưng đến spa ngay nhé!</p>";

                    await _emailService.SendEmailAsync(user.Email, "Chúc Mừng Sinh Nhật Từ AutoWashPro", emailHtml);
                }
            }
        }

        public async Task RunWinbackCampaignAsync()
        {
            _logger.LogInformation("Bắt đầu quét khách hàng Win-back (>60 ngày)...");

            var thresholdDate = DateTime.UtcNow.ToVnTime().AddDays(-60).Date;
            var winbackCutoffDate = DateTime.UtcNow.ToVnTime().AddDays(-30).Date;

            var winbackUsers = await _context.Users
                .Include(u => u.CustomerProfile)
                .Where(u => u.CustomerProfile != null
                            && u.CustomerProfile.LastVisitDate.HasValue
                            && u.CustomerProfile.LastVisitDate.Value.Date <= thresholdDate
                            && (u.CustomerProfile.LastWinbackSentDate == null || u.CustomerProfile.LastWinbackSentDate.Value.Date <= winbackCutoffDate)
                            && u.Status == "Active")
                .ToListAsync();

            if (!winbackUsers.Any())
            {
                _logger.LogInformation("Không có khách hàng nào vào diện Win-back hôm nay.");
                return;
            }

            foreach (var user in winbackUsers)
            {
                var code = $"COMEBACK-{user.UserId}-{DateTime.UtcNow.ToVnTime():MMdd}";

                var hasVoucher = await _context.Vouchers.AnyAsync(v => v.Code == code);
                if (hasVoucher) continue;

                var voucher = new Voucher
                {
                    Code = code,
                    DiscountAmount = 30000,
                    MaxUsages = 1,
                    PointsRequired = 0,
                    ExpiryDate = DateTime.UtcNow.AddDays(14),
                    VoucherType = AutoWashPro.DAL.Enums.VoucherType.Discount
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                var userVoucher = new UserVoucher
                {
                    UserId = user.UserId,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);

                user.CustomerProfile.LastWinbackSentDate = DateTime.UtcNow.ToVnTime().Date;

                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogInformation($"Gửi email Win-back cho User {user.UserId} ({user.Email})");
                    var emailHtml = $@"
                        <h3>Chào {user.CustomerProfile.FullName}, lâu rồi không gặp!</h3>
                        <p>AutoWashPro rất nhớ bạn và xế cưng. Để mời bạn quay lại trải nghiệm dịch vụ, chúng tôi gửi tặng bạn:</p>
                        <p><b>Voucher Giảm 30,000đ</b> (Mã: {code}) đã được tự động thêm vào ví của bạn.</p>
                        <p>Hạn sử dụng: 14 ngày kể từ hôm nay.</p>
                        <p>AutoWashPro tặng bạn 30K, hẹn lịch mang xế cưng đến spa ngay nhé!</p>";

                    await _emailService.SendEmailAsync(user.Email, "AutoWashPro Tặng Bạn 30K - Quay Lại Ngay Nhé!", emailHtml);
                }
            }
        }
    }
}
