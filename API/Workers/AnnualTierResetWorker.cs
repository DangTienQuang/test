using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AutoWashPro.DAL.Data;

namespace AutoWashPro.API.Workers
{
    public class AnnualTierResetWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AnnualTierResetWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                // Chạy vào 23:59 ngày 31/12
                if (now.Month == 12 && now.Day == 31 && now.Hour == 23)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AutoWashDbContext>();

                    var allProfiles = await context.CustomerProfiles.ToListAsync(stoppingToken);
                    var allTiers = await context.Tiers.OrderByDescending(t => t.MinAccumulatedPoints).ToListAsync(stoppingToken);

                    foreach (var profile in allProfiles)
                    {
                        var newTier = allTiers.FirstOrDefault(t => profile.CurrentYearTierPoints >= t.MinAccumulatedPoints);
                        if (newTier != null)
                        {
                            profile.TierId = newTier.TierId;
                        }

                        // Reset điểm cống hiến về 0 cho năm mới
                        profile.CurrentYearTierPoints = 0;
                    }

                    await context.SaveChangesAsync(stoppingToken);

                    // Ngủ 24h để tránh chạy lại liên tục trong cùng ngày 31/12
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }

                // Kiểm tra mỗi giờ 1 lần
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
