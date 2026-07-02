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

namespace AutoWashPro.BLL.Services
{
    public class CRMCampaignService : ICRMCampaignService
    {
        private readonly IVoucherCampaignService _voucherCampaignService;
        private readonly IWeatherService _weatherService;
        private readonly AutoWashDbContext _context;

        public CRMCampaignService(
            IVoucherCampaignService voucherCampaignService,
            IWeatherService weatherService,
            AutoWashDbContext context)
        {
            _voucherCampaignService = voucherCampaignService;
            _weatherService = weatherService;
            _context = context;
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
    }
}
