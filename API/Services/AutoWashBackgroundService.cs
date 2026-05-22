using AutoWashPro.BLL.Services;
using AutoWashPro.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWashPro.API.Services
{
    public class AutoWashBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoWashBackgroundService> _logger;

        public AutoWashBackgroundService(IServiceProvider serviceProvider, ILogger<AutoWashBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBackgroundTasksAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong quá trình chạy Background Service");
                }

                // Chạy mỗi 5 phút
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessBackgroundTasksAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AutoWashDbContext>();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var tierService = scope.ServiceProvider.GetRequiredService<ITierService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;

            // 1. Hủy lịch hẹn quá 15 phút (Đến trễ)
            var expiredBookings = await context.Bookings
                .Where(b => b.Status == "Pending" && b.ScheduledTime.AddMinutes(15) < now)
                .ToListAsync();

            foreach (var booking in expiredBookings)
            {
                // Nếu quá 15 phút thì tự động đổi status, giữ lại cọc theo rule "Đến trễ/Hủy lịch sát giờ"
                booking.Status = "Cancelled";
                booking.UpdatedAt = now;
                // Thêm vào Transaction log nếu cần để ghi nhận giữ cọc
                _logger.LogInformation($"Auto-cancelled booking {booking.BookingId} due to 15m delay.");
            }
            if (expiredBookings.Any())
            {
                await context.SaveChangesAsync();
            }

            // 2. Xét duyệt nâng/hạ hạng định kỳ vào ngày 1 hàng tháng
            // Check nếu chưa chạy nâng hạng cho tháng này
            // Đơn giản hóa: Chạy kiểm tra mỗi ngày 1 lúc đầu ngày
            if (now.Day == 1 && now.Hour == 0 && now.Minute < 5)
            {
                var users = await context.Users.Select(u => u.UserId).ToListAsync();
                foreach (var userId in users)
                {
                    await tierService.EvaluateAndUpgradeTierAsync(userId);
                }
                _logger.LogInformation("Đã chạy xét duyệt nâng/hạ hạng đầu tháng.");
            }

            // 3. Nhắc hẹn tự động trước 24h và 2h
            var upcomingBookings24h = await context.Bookings
                .Include(b => b.User)
                .Where(b => b.Status == "Pending" && b.ScheduledTime > now.AddHours(23).AddMinutes(55) && b.ScheduledTime <= now.AddHours(24))
                .ToListAsync();

            foreach (var b in upcomingBookings24h)
            {
                if (!string.IsNullOrEmpty(b.User.Email))
                {
                    await emailService.SendEmailAsync(b.User.Email, $"[SmartWash] Nhắc nhở: Bạn có lịch hẹn rửa xe trong 24h tới", $"Lịch hẹn của bạn sẽ bắt đầu lúc {b.ScheduledTime:dd/MM/yyyy HH:mm}. Vui lòng đến đúng giờ.");
                }
            }

            var upcomingBookings2h = await context.Bookings
                .Include(b => b.User)
                .Where(b => b.Status == "Pending" && b.ScheduledTime > now.AddHours(1).AddMinutes(55) && b.ScheduledTime <= now.AddHours(2))
                .ToListAsync();

            foreach (var b in upcomingBookings2h)
            {
                if (!string.IsNullOrEmpty(b.User.Email))
                {
                    await emailService.SendEmailAsync(b.User.Email, $"[SmartWash] Nhắc nhở: Bạn có lịch hẹn rửa xe trong 2h tới", $"Lịch hẹn của bạn sẽ bắt đầu lúc {b.ScheduledTime:dd/MM/yyyy HH:mm}. Vui lòng đến đúng giờ.");
                }
            }

            // 4. Gửi ưu đãi tự động cho khách không quay lại sau 30 ngày
            var inactiveProfiles = await context.CustomerProfiles
                .Include(cp => cp.User)
                .Where(cp => cp.LastVisitDate.HasValue && cp.LastVisitDate.Value.AddDays(30) < now && cp.LastVisitDate.Value.AddDays(30).AddMinutes(5) >= now)
                .ToListAsync();

            foreach (var cp in inactiveProfiles)
            {
                if (!string.IsNullOrEmpty(cp.User.Email))
                {
                    await emailService.SendEmailAsync(cp.User.Email, $"[SmartWash] Ưu đãi đặc biệt dành cho bạn!", $"Đã lâu rồi bạn chưa ghé SmartWash. Chúng tôi tặng bạn Voucher giảm giá 20% cho lần sử dụng tiếp theo.");
                }
            }
        }
    }
}
