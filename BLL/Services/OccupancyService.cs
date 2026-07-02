using System;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Services.Interface;
using AutoWashPro.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.BLL.Services
{
    public class OccupancyService : IOccupancyService
    {
        private readonly AutoWashDbContext _context;

        public OccupancyService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<double> GetBranchOccupancyRateAsync(int branchId, DateTime targetDate)
        {
            // targetDate is typically DateTime.UtcNow (or equivalent local time)
            var targetDateVn = targetDate;
            var targetTimeOfDay = targetDateVn.TimeOfDay;
            var endTargetTimeOfDay = targetTimeOfDay.Add(TimeSpan.FromHours(4));

            var dailyCapacities = await _context.DailySlotCapacities
                .Include(dsc => dsc.TimeSlot)
                .Where(dsc => dsc.BranchId == branchId && dsc.Date == targetDateVn.Date)
                .ToListAsync();

            // Filter for next 4 hours
            var relevantCapacities = dailyCapacities
                .Where(dsc => dsc.TimeSlot.StartTime >= targetTimeOfDay && dsc.TimeSlot.StartTime < endTargetTimeOfDay)
                .ToList();

            if (!relevantCapacities.Any())
            {
                return 0.0;
            }

            int totalBookedWeight = relevantCapacities.Sum(dsc => dsc.BookedWeight);
            int totalMaxCapacity = relevantCapacities.Sum(dsc => dsc.TimeSlot.MaxCapacity);

            if (totalMaxCapacity == 0)
            {
                return 0.0;
            }

            return (double)totalBookedWeight / totalMaxCapacity;
        }
    }
}
