using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AutoWashPro.BLL.Services;
using AutoWashPro.BLL.Helpers;

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
                        var campaignService = scope.ServiceProvider.GetRequiredService<ICRMCampaignService>();

                        await campaignService.RunBirthdayCampaignAsync();
                        await campaignService.RunWinbackCampaignAsync();
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
    }
}
