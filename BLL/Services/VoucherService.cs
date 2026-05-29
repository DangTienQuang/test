using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using AutoWashPro.BLL.Exceptions;

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
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var voucher = await _context.Vouchers.FindAsync(voucherId);
                if (voucher == null) throw new NotFoundException("Voucher không tồn tại.");
                if (voucher.ExpiryDate < DateTime.UtcNow) throw new BadRequestException("Voucher đã hết hạn.");

                if (voucher.MaxUsages > 0)
                {
                    var usageCount = await _context.UserVouchers.CountAsync(uv => uv.VoucherId == voucherId);
                    if (usageCount >= voucher.MaxUsages)
                        throw new BadRequestException("Voucher đã hết lượt đổi.");
                }

                var existingUserVoucher = await _context.UserVouchers
                    .FirstOrDefaultAsync(uv => uv.UserId == userId && uv.VoucherId == voucherId);
                if (existingUserVoucher != null) throw new BadRequestException("Bạn đã sở hữu voucher này rồi.");

                await _walletService.DeductSpendablePointsAsync(userId, voucher.PointsRequired, $"Đổi voucher: {voucher.Code}");

                var userVoucher = new UserVoucher
                {
                    UserId = userId,
                    VoucherId = voucherId,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                throw new BadRequestException("Bạn đã sở hữu voucher này rồi, không thể thao tác quá nhanh.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<AdminVoucherDTO>> GetAllVouchersAsync()
        {
            var vouchers = await _context.Vouchers.OrderByDescending(v => v.ExpiryDate).ToListAsync();
            var redeemCounts = await _context.UserVouchers
                .GroupBy(uv => uv.VoucherId)
                .Select(g => new { VoucherId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.VoucherId, x => x.Count);

            return vouchers.Select(v => new AdminVoucherDTO
            {
                VoucherId = v.VoucherId,
                Code = v.Code,
                DiscountAmount = v.DiscountAmount,
                MaxUsages = v.MaxUsages,
                ExpiryDate = v.ExpiryDate,
                PointsRequired = v.PointsRequired,
                RedeemedCount = redeemCounts.GetValueOrDefault(v.VoucherId, 0)
            }).ToList();
        }

        public async Task<AdminVoucherDTO> CreateVoucherAsync(CreateOrUpdateVoucherDTO request)
        {
            if (request.ExpiryDate.ToUniversalTime() <= DateTime.UtcNow) throw new BadRequestException("Ngày hết hạn phải lớn hơn hiện tại.");

            var codeExists = await _context.Vouchers.AnyAsync(v => v.Code == request.Code.Trim());
            if (codeExists) throw new BadRequestException("Mã voucher đã tồn tại.");

            var voucher = new Voucher
            {
                Code = request.Code.Trim(),
                DiscountAmount = request.DiscountAmount,
                MaxUsages = request.MaxUsages,
                ExpiryDate = request.ExpiryDate.ToUniversalTime(),
                PointsRequired = request.PointsRequired
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return MapAdminDto(voucher, 0);
        }

        public async Task<AdminVoucherDTO> UpdateVoucherAsync(int id, CreateOrUpdateVoucherDTO request)
        {
            if (request.ExpiryDate.ToUniversalTime() <= DateTime.UtcNow) throw new BadRequestException("Ngày hết hạn phải lớn hơn hiện tại.");

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) throw new NotFoundException("Không tìm thấy voucher.");

            var codeExists = await _context.Vouchers.AnyAsync(v => v.Code == request.Code.Trim() && v.VoucherId != id);
            if (codeExists) throw new BadRequestException("Mã voucher đã tồn tại.");

            voucher.Code = request.Code.Trim();
            voucher.DiscountAmount = request.DiscountAmount;
            voucher.MaxUsages = request.MaxUsages;
            voucher.ExpiryDate = request.ExpiryDate.ToUniversalTime();
            voucher.PointsRequired = request.PointsRequired;

            await _context.SaveChangesAsync();

            var redeemCount = await _context.UserVouchers.CountAsync(uv => uv.VoucherId == id);
            return MapAdminDto(voucher, redeemCount);
        }

        public async Task<bool> DeleteVoucherAsync(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) throw new NotFoundException("Không tìm thấy voucher.");

            var hasOwners = await _context.UserVouchers.AnyAsync(uv => uv.VoucherId == id);
            if (hasOwners)
                throw new BadRequestException("Không thể xóa voucher đã có khách đổi. Vui lòng để hết hạn hoặc ngừng phát hành.");

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return true;
        }

        private static AdminVoucherDTO MapAdminDto(Voucher v, int redeemedCount) => new()
        {
            VoucherId = v.VoucherId,
            Code = v.Code,
            DiscountAmount = v.DiscountAmount,
            MaxUsages = v.MaxUsages,
            ExpiryDate = v.ExpiryDate,
            PointsRequired = v.PointsRequired,
            RedeemedCount = redeemedCount
        };
        public async Task GenerateCompensationVoucherAsync(int userId)
        {
            var code = $"SORRY-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

            var voucher = new Voucher
            {
                Code = code,
                DiscountAmount = 30000, // 30,000 VND discount
                MaxUsages = 1,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                PointsRequired = 0
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            var userVoucher = new UserVoucher
            {
                UserId = userId,
                VoucherId = voucher.VoucherId,
                IsUsed = false
            };

            _context.UserVouchers.Add(userVoucher);
            await _context.SaveChangesAsync();
        }
    }
}
