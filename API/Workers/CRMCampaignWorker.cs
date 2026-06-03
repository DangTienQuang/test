using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using AutoWashPro.BLL.Services;

namespace AutoWashPro.API.Workers
{
    public class CRMCampaignWorker : BackgroundService
    {
        private readonly ILogger<CRMCampaignWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CRMCampaignWorker(ILogger<CRMCampaignWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CRM Campaign Worker bắt đầu khởi chạy.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                if (now.Hour == 17 && now.Minute == 0) // Chạy vào lúc 0h00 giờ VN (UTC+7) -> 17h00 UTC
                {
                    _logger.LogInformation("Đến giờ chạy CRM Campaign hằng ngày...");

                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AutoWashDbContext>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        await RunBirthdayCampaignAsync(context, emailService);
                        await RunWinbackCampaignAsync(context, emailService);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi chạy CRM Campaign Worker.");
                    }

                    await Task.Delay(TimeSpan.FromHours(23), stoppingToken); // Chờ 23h để tránh chạy lại trong cùng 1 phút, sau đó vòng lặp sẽ check mỗi phút
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task RunBirthdayCampaignAsync(AutoWashDbContext context, IEmailService emailService)
        {
            _logger.LogInformation("Bắt đầu quét khách hàng có sinh nhật hôm nay...");

            var today = DateTime.UtcNow.AddHours(7).Date; // Lấy ngày VN

            var birthdayUsers = await context.Users
                .Include(u => u.CustomerProfile)
                .Where(u => u.CustomerProfile != null
                            && u.CustomerProfile.DateOfBirth.HasValue
                            && u.CustomerProfile.DateOfBirth.Value.Month == today.Month
                            && u.CustomerProfile.DateOfBirth.Value.Day == today.Day
                            && u.Status == "Active")
                .ToListAsync();

            if (!birthdayUsers.Any())
            {
                _logger.LogInformation("Không có khách hàng nào sinh nhật hôm nay.");
                return;
            }

            foreach (var user in birthdayUsers)
            {
                var code = $"HPBD-{user.UserId}-{today.Year}";

                // Kiểm tra xem đã tặng voucher sinh nhật năm nay chưa
                var hasVoucher = await context.Vouchers.AnyAsync(v => v.Code == code);
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

                context.Vouchers.Add(voucher);
                await context.SaveChangesAsync();

                var userVoucher = new UserVoucher
                {
                    UserId = user.UserId,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false
                };

                context.UserVouchers.Add(userVoucher);
                await context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogInformation($"Gửi email chúc mừng sinh nhật cho User {user.UserId} ({user.Email})");
                    var emailHtml = $@"
                        <h3>Chúc mừng sinh nhật, {user.CustomerProfile.FullName}!</h3>
                        <p>AutoWashPro gửi tặng bạn một phần quà sinh nhật:</p>
                        <p><b>Voucher Giảm 50,000đ</b> (Mã: {code}) đã được thêm vào ví của bạn.</p>
                        <p>Hạn sử dụng: 7 ngày kể từ hôm nay.</p>
                        <p>Hãy đặt lịch và mang xế cưng đến spa ngay nhé!</p>";

                    await emailService.SendEmailAsync(user.Email, "Chúc Mừng Sinh Nhật Từ AutoWashPro", emailHtml);
                }
            }
        }

        private async Task RunWinbackCampaignAsync(AutoWashDbContext context, IEmailService emailService)
        {
            _logger.LogInformation("Bắt đầu quét khách hàng Win-back (>60 ngày)...");

            var targetDate = DateTime.UtcNow.AddDays(-60);

            var winbackUsers = await context.Users
                .Include(u => u.CustomerProfile)
                .Where(u => u.CustomerProfile != null
                            && u.CustomerProfile.LastVisitDate.HasValue
                            && u.CustomerProfile.LastVisitDate.Value.Date == targetDate.Date // Chạy đúng vào ngày thứ 60
                            && u.Status == "Active")
                .ToListAsync();

            if (!winbackUsers.Any())
            {
                _logger.LogInformation("Không có khách hàng nào vào diện Win-back hôm nay.");
                return;
            }

            foreach (var user in winbackUsers)
            {
                var code = $"COMEBACK-{user.UserId}-{DateTime.UtcNow:MMdd}";

                var hasVoucher = await context.Vouchers.AnyAsync(v => v.Code == code);
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

                context.Vouchers.Add(voucher);
                await context.SaveChangesAsync();

                var userVoucher = new UserVoucher
                {
                    UserId = user.UserId,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false
                };

                context.UserVouchers.Add(userVoucher);
                await context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogInformation($"Gửi email Win-back cho User {user.UserId} ({user.Email})");
                    var emailHtml = $@"
                        <h3>Chào {user.CustomerProfile.FullName}, lâu rồi không gặp!</h3>
                        <p>AutoWashPro rất nhớ bạn và xế cưng. Để mời bạn quay lại trải nghiệm dịch vụ, chúng tôi gửi tặng bạn:</p>
                        <p><b>Voucher Giảm 30,000đ</b> (Mã: {code}) đã được tự động thêm vào ví của bạn.</p>
                        <p>Hạn sử dụng: 14 ngày kể từ hôm nay.</p>
                        <p>AutoWashPro tặng bạn 30K, hẹn lịch mang xế cưng đến spa ngay nhé!</p>";

                    await emailService.SendEmailAsync(user.Email, "AutoWashPro Tặng Bạn 30K - Quay Lại Ngay Nhé!", emailHtml);
                }
            }
        }
    }
}