using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.BLL.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly AutoWashDbContext _context;
        private readonly IWalletService _walletService;

        public VoucherService(AutoWashDbContext context, IWalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        public async Task<List<VoucherResponseDTO>> GetMyVouchersAsync(int userId)
        {
            return await _context.UserVouchers
                .Include(uv => uv.Voucher)
                .Where(uv => uv.UserId == userId)
                .Select(uv => new VoucherResponseDTO
                {
                    VoucherId = uv.VoucherId,
                    Code = uv.Voucher.Code,
                    DiscountAmount = uv.Voucher.DiscountAmount,
                    PointsRequired = uv.Voucher.PointsRequired,
                    ExpiryDate = uv.Voucher.ExpiryDate,
                    IsUsed = uv.IsUsed,
                    UsedDate = uv.UsedDate
                }).ToListAsync();
        }

        public async Task RedeemVoucherAsync(int userId, int voucherId)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null) throw new Exception("Voucher không tồn tại.");
            if (voucher.ExpiryDate < DateTime.UtcNow) throw new Exception("Voucher đã hết hạn.");

            var existingUserVoucher = await _context.UserVouchers
                .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
            if (existingUserVoucher != null) throw new Exception("Bạn đã sở hữu voucher này rồi.");

            await _walletService.DeductPointsFIFOAsync(userId, voucher.PointsRequired, $"Đổi voucher: {voucher.Code}");

            var userVoucher = new UserVoucher
            {
                UserId = userId,
                VoucherId = voucherId,
                IsUsed = false
            };

            _context.UserVouchers.Add(userVoucher);
            await _context.SaveChangesAsync();
        }
    }
}
