using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services.Interface;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using AutoWashPro.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoWashPro.BLL.Services
{
    public class CRMCampaignService : ICRMCampaignService
    {
        private readonly IVoucherCampaignService _voucherCampaignService;
        private readonly IWeatherService _weatherService;
        private readonly IOccupancyService _occupancyService;
        private readonly AutoWashDbContext _context;
        private readonly ILogger<CRMCampaignService> _logger;

        public CRMCampaignService(
            IVoucherCampaignService voucherCampaignService,
            IWeatherService weatherService,
            IOccupancyService occupancyService,
            AutoWashDbContext context,
            ILogger<CRMCampaignService> logger)
        {
            _voucherCampaignService = voucherCampaignService;
            _weatherService = weatherService;
            _occupancyService = occupancyService;
            _context = context;
            _logger = logger;
        }

        public async Task<List<VoucherCampaignProcessResultDTO>> ProcessDailyCampaignsAsync()
        {
            return await _voucherCampaignService.ProcessDailyCampaignsAsync();
        }

        public async Task<string> TriggerWeatherCampaignAsync()
        {
            var isRaining = await _weatherService.IsRainingNowAsync();
            if (!isRaining)
            {
                return "Weather is clear. No campaign triggered.";
            }

            var voucherCode = "RAINYDAY30";
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == voucherCode);

            if (voucher == null)
            {
                voucher = new Voucher
                {
                    Code = voucherCode,
                    DiscountAmount = 30,
                    VoucherType = VoucherType.Discount,
                    CampaignType = VoucherCampaignType.Weather,
                    ExpiryDays = 1,
                    IsActive = true,
                    MaxUsagePerUser = 1,
                    MaxUsages = 999999, // Large number to prevent it from running out
                    StartDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddYears(1) // General template expiry, UserVoucher will enforce 1 day
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();
            }

            var activeUsers = await _context.Users.Where(u => u.Status == "Active").ToListAsync();
            var today = DateTime.UtcNow.Date;
            var assignedCount = 0;

            var usersReceivedToday = await _context.UserVouchers
                .Where(uv => uv.VoucherId == voucher.VoucherId && uv.ReceivedDate.Date == today)
                .Select(uv => uv.UserId)
                .ToListAsync();

            var receivedUserIds = new HashSet<int>(usersReceivedToday);

            foreach (var user in activeUsers)
            {
                if (!receivedUserIds.Contains(user.UserId))
                {
                    var userVoucher = new UserVoucher
                    {
                        UserId = user.UserId,
                        VoucherId = voucher.VoucherId,
                        ReceivedDate = DateTime.UtcNow,
                        ExpiryDate = DateTime.UtcNow.AddDays(1),
                        IsUsed = false,
                        TriggerKey = "WeatherCampaign"
                    };

                    _context.UserVouchers.Add(userVoucher);
                    assignedCount++;
                }
            }

            if (assignedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return $"Rain detected. Rainy day voucher assigned to {assignedCount} users.";
        }

        public async Task<string> TriggerSmartWeatherCampaignAsync()
        {
            var targetDate = DateTime.UtcNow;
            var today = targetDate.Date;
            var branches = await _context.Branches.Where(b => b.IsActive).ToListAsync();
            int totalVouchersIssued = 0;

            // Ensure the Scenario exists
            var scenario = await _context.KnowledgeScenarios.FirstOrDefaultAsync(s => s.ScenarioCode == "WEATHER_CAMPAIGN");
            if (scenario == null)
            {
                // Find or create a category
                var category = await _context.KnowledgeCategories.FirstOrDefaultAsync();
                if (category == null)
                {
                    category = new KnowledgeCategory { Name = "Campaigns", Code = "CAMPAIGNS", Description = "Campaigns Category" };
                    _context.KnowledgeCategories.Add(category);
                    await _context.SaveChangesAsync();
                }

                scenario = new KnowledgeScenario
                {
                    ScenarioCode = "WEATHER_CAMPAIGN",
                    ScenarioName = "Smart Weather Campaign",
                    Enabled = true,
                    CategoryId = category.CategoryId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.KnowledgeScenarios.Add(scenario);
                await _context.SaveChangesAsync();
            }

            foreach (var branch in branches)
            {
                var occupancyRate = await _occupancyService.GetBranchOccupancyRateAsync(branch.BranchId, targetDate);
                if (occupancyRate > 0.50)
                {
                    _logger.LogInformation("Branch {BranchId} has occupancy rate {OccupancyRate:P2}. Skipping smart weather campaign.", branch.BranchId, occupancyRate);
                    continue;
                }

                var isProlongedRain = await _weatherService.IsProlongedRainAsync(branch);
                if (!isProlongedRain)
                {
                    _logger.LogInformation("Branch {BranchId} does not have prolonged rain. Skipping smart weather campaign.", branch.BranchId);
                    continue;
                }

                string voucherCode = $"RAIN_BR{branch.BranchId}";
                var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == voucherCode);

                if (voucher == null)
                {
                    voucher = new Voucher
                    {
                        Code = voucherCode,
                        DiscountAmount = 30,
                        VoucherType = VoucherType.Discount,
                        CampaignType = VoucherCampaignType.Weather,
                        ExpiryDays = 1,
                        IsActive = true,
                        MaxUsagePerUser = 1,
                        MaxUsages = 999999,
                        StartDate = DateTime.UtcNow,
                        ExpiryDate = DateTime.UtcNow.AddYears(1)
                    };
                    _context.Vouchers.Add(voucher);
                    await _context.SaveChangesAsync();
                }

                // Find loyal users for this branch
                var targetCustomers = await _context.CustomerFeatureProfiles
                    .Where(cfp => cfp.FavoriteBranchId == branch.BranchId && cfp.Customer.Status == "Active")
                    .Select(cfp => cfp.CustomerId)
                    .ToListAsync();

                if (!targetCustomers.Any())
                {
                    continue;
                }

                // Exclude users who already received this voucher today
                var usersReceivedToday = await _context.UserVouchers
                    .Where(uv => uv.VoucherId == voucher.VoucherId && uv.ReceivedDate.Date == today)
                    .Select(uv => uv.UserId)
                    .ToListAsync();

                var receivedUserIds = new HashSet<int>(usersReceivedToday);
                int branchAssignedCount = 0;

                foreach (var customerId in targetCustomers)
                {
                    if (!receivedUserIds.Contains(customerId))
                    {
                        var userVoucher = new UserVoucher
                        {
                            UserId = customerId,
                            VoucherId = voucher.VoucherId,
                            ReceivedDate = DateTime.UtcNow,
                            ExpiryDate = DateTime.UtcNow.AddDays(1),
                            IsUsed = false,
                            TriggerKey = "SmartWeatherCampaign"
                        };
                        _context.UserVouchers.Add(userVoucher);

                        var decisionHistory = new AIDecisionHistory
                        {
                            CustomerId = customerId,
                            ScenarioId = scenario.ScenarioId,
                            VoucherId = voucher.VoucherId,
                            ActionType = "Issue Weather Voucher",
                            DecisionReason = "Prolonged rain + Occupancy below 50%",
                            Confidence = 0.95,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.AIDecisionHistories.Add(decisionHistory);

                        branchAssignedCount++;
                        totalVouchersIssued++;
                    }
                }

                if(branchAssignedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }

            return $"Smart Weather Campaign executed. Issued {totalVouchersIssued} branch-specific vouchers.";
        }
    }
}
