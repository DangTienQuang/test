using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class TierService : ITierService
    {
        private readonly AutoWashDbContext _context;

        public TierService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<TierResponseDTO> CreateTierAsync(CreateTierDTO request)
        {
            var tier = new Tier
            {
                TierName = request.TierName,
                PointMultiplier = request.PointMultiplier,
                BookingWindowDays = request.BookingWindowDays
            };

            _context.Tiers.Add(tier);
            await _context.SaveChangesAsync();

            return new TierResponseDTO
            {
                TierId = tier.TierId,
                TierName = tier.TierName,
                PointMultiplier = tier.PointMultiplier,
                BookingWindowDays = tier.BookingWindowDays
            };
        }

        public async Task<List<TierResponseDTO>> GetAllTiersAsync()
        {
            var tiers = await _context.Tiers
                .Select(t => new TierResponseDTO
                {
                    TierId = t.TierId,
                    TierName = t.TierName,
                    PointMultiplier = t.PointMultiplier,
                    BookingWindowDays = t.BookingWindowDays
                }).ToListAsync();

            return tiers;
        }
    }
}
