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
    public class TierService : ITierService
    {
        private readonly AutoWashDbContext _context;

        public TierService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<List<TierResponseDTO>> GetTiersAsync()
        {
            return await _context.Tiers
                .OrderBy(t => t.MinAccumulatedPoints)
                .Select(t => new TierResponseDTO
                {
                    TierId = t.TierId,
                    TierName = t.TierName,
                    PointMultiplier = t.PointMultiplier,
                    BookingWindowDays = t.BookingWindowDays,
                    MinAccumulatedPoints = t.MinAccumulatedPoints
                }).ToListAsync();
        }

        public async Task<TierResponseDTO> CreateTierAsync(CreateTierDTO request)
        {
            var isDuplicate = await _context.Tiers.AnyAsync(t => t.TierName == request.TierName || t.MinAccumulatedPoints == request.MinAccumulatedPoints);
            if (isDuplicate) throw new BadRequestException("Tên hạng hoặc số điểm tối thiểu đã tồn tại.");

            var tier = new Tier
            {
                TierName = request.TierName,
                PointMultiplier = request.PointMultiplier,
                BookingWindowDays = request.BookingWindowDays,
                MinAccumulatedPoints = request.MinAccumulatedPoints
            };

            _context.Tiers.Add(tier);
            await _context.SaveChangesAsync();

            return await GetTierById(tier.TierId);
        }

        public async Task<TierResponseDTO> UpdateTierAsync(int id, UpdateTierDTO request)
        {
            var tier = await _context.Tiers.FindAsync(id);
            if (tier == null) throw new NotFoundException("Không tìm thấy hạng thành viên.");

            var isDuplicate = await _context.Tiers.AnyAsync(t => (t.TierName == request.TierName || t.MinAccumulatedPoints == request.MinAccumulatedPoints) && t.TierId != id);
            if (isDuplicate) throw new BadRequestException("Tên hạng hoặc số điểm tối thiểu đã tồn tại.");

            tier.TierName = request.TierName;
            tier.PointMultiplier = request.PointMultiplier;
            tier.BookingWindowDays = request.BookingWindowDays;
            tier.MinAccumulatedPoints = request.MinAccumulatedPoints;

            await _context.SaveChangesAsync();
            return await GetTierById(tier.TierId);
        }

        public async Task<TierUpgradeResultDTO?> EvaluateAndUpgradeTierAsync(int userId)
        {
            var profile = await _context.CustomerProfiles
                .Include(cp => cp.Tier)
                .FirstOrDefaultAsync(cp => cp.UserId == userId);

            if (profile?.Tier == null) return null;

            var eligibleTier = await _context.Tiers
                .Where(t => t.MinAccumulatedPoints <= profile.PromotionPoint)
                .OrderByDescending(t => t.MinAccumulatedPoints)
                .FirstOrDefaultAsync();

            if (eligibleTier == null || eligibleTier.TierId == profile.TierId)
                return null;

            if (eligibleTier.MinAccumulatedPoints <= profile.Tier.MinAccumulatedPoints)
                return null;

            var oldTierName = profile.Tier.TierName;
            profile.TierId = eligibleTier.TierId;
            await _context.SaveChangesAsync();

            return new TierUpgradeResultDTO
            {
                OldTierName = oldTierName,
                NewTierName = eligibleTier.TierName
            };
        }

        private async Task<TierResponseDTO> GetTierById(int id)
        {
            var t = await _context.Tiers.FindAsync(id);
            return new TierResponseDTO
            {
                TierId = t.TierId,
                TierName = t.TierName,
                PointMultiplier = t.PointMultiplier,
                BookingWindowDays = t.BookingWindowDays,
                MinAccumulatedPoints = t.MinAccumulatedPoints
            };
        }
    }
}