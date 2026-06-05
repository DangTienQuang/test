using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoWashPro.DAL.Data;

namespace AutoWashPro.BLL.Services
{
    public class AnnualTierService : IAnnualTierService
    {
        private readonly AutoWashDbContext _context;

        public AnnualTierService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task ResetAnnualTiersAsync()
        {
            var allProfiles = await _context.CustomerProfiles.ToListAsync();
            var allTiers = await _context.Tiers.OrderByDescending(t => t.MinAccumulatedPoints).ToListAsync();

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

            await _context.SaveChangesAsync();
        }
    }
}
