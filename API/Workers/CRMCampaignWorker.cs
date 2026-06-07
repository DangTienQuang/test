using AutoWashPro.BLL.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWashPro.API.Workers
{
    public class CRMCampaignWorker : BackgroundService
    {
        private readonly ILogger<CRMCampaignWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public CRMCampaignWorker(ILogger<CRMCampaignWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CRM Campaign Worker started.");
            var intervalSeconds = _configuration.GetValue<int?>("VoucherCampaignWorker:IntervalSeconds");

            if (intervalSeconds.HasValue && intervalSeconds.Value > 0)
            {
                await ExecuteIntervalModeAsync(TimeSpan.FromSeconds(intervalSeconds.Value), stoppingToken);
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;

                if (now.Hour == 17 && now.Minute == 0)
                {
                    await ProcessDailyCampaignsAsync();
                    await Task.Delay(TimeSpan.FromHours(23), stoppingToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ExecuteIntervalModeAsync(TimeSpan interval, CancellationToken stoppingToken)
        {
            _logger.LogInformation("CRM Campaign Worker interval mode enabled. Interval: {IntervalSeconds}s", interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessDailyCampaignsAsync();
                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task ProcessDailyCampaignsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crmCampaignService = scope.ServiceProvider.GetRequiredService<ICRMCampaignService>();
                var results = await crmCampaignService.ProcessDailyCampaignsAsync();

                foreach (var result in results)
                {
                    _logger.LogInformation(
                        "Processed voucher campaign {CampaignType}/{Code}: scanned={Scanned}, granted={Granted}, skipped={Skipped}",
                        result.CampaignType,
                        result.VoucherCode,
                        result.ScannedUsers,
                        result.GrantedCount,
                        result.SkippedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing daily voucher campaigns.");
            }
        }
    }
}
