using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using BLL.DTOs.Business;
using BLL.DTOs.Fleet;
using BLL.Helpers;
using BLL.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services
{
    public class LaneSchedulerService : ILaneSchedulerService
    {
        private readonly AutoWashDbContext _context;

        // Grace window past slot end before we reject the booking.
        // Absorbs real-world arrival variance without cascading into the next slot.
        private const int SlotGraceMinutes = 15;

        public LaneSchedulerService(AutoWashDbContext context)
        {
            _context = context;
        }

        // ── Public: projection ───────────────────────────────────────────────

        public async Task<Dictionary<int, DateTime>> GetLaneProjectedFreeTimesAsync(int branchId, DateTime slotStart)
        {
            var lanes = await _context.Lanes
                .Where(x => x.BranchId == branchId && x.IsActive)
                .ToListAsync();

            // All logs that are still in progress at this branch
            var activeLogs = await _context.FleetWashLogs
                .Include(x => x.FleetVehicle)
                    .ThenInclude(x => x.VehicleType)
                .Where(x =>
                    x.BranchId == branchId &&
                    (x.Status == "CheckedIn" ||
                     x.Status == "Assigned" ||
                     x.Status == "Processing"))
                .ToListAsync();

            var result = new Dictionary<int, DateTime>();

            foreach (var lane in lanes)
            {
                var logsOnLane = activeLogs
                    .Where(x => x.LaneId == lane.LaneId)
                    .OrderByDescending(x => x.CheckInTime)
                    .ToList();

                if (!logsOnLane.Any())
                {
                    // Lane is idle — free immediately from slot start
                    result[lane.LaneId] = slotStart;
                    continue;
                }

                // For the latest log on this lane, estimate when it finishes.
                // We derive duration from the vehicle's weight + the service prices
                // at that branch — no new DB columns needed.
                var latestLog = logsOnLane.First();
                var vehicleType = latestLog.FleetVehicle?.VehicleType;
                int baseWeight = vehicleType?.BaseWeight ?? 1;

                var servicePrices = await _context.ServicePrices
                    .Where(x =>
                        x.BranchId == branchId &&
                        x.VehicleTypeId == (vehicleType != null ? vehicleType.Id : 0))
                    .ToListAsync();

                int estimatedMinutes = WashTimeEstimator.EstimateMinutes(servicePrices);

                // ProjectedFreeAt = when the wash started + how long it takes
                // If already in Processing, use CheckInTime as the start baseline
                result[lane.LaneId] = latestLog.CheckInTime.AddMinutes(estimatedMinutes);
            }

            return result;
        }

        // ── Public: EAL simulation ───────────────────────────────────────────

        public async Task<LaneScheduleResult> ScheduleFleetAsync(int branchId, DateTime slotStart, TimeSpan slotDuration, List<VehicleScheduleRequest> vehicles)
        {
            if (!vehicles.Any())
                return LaneScheduleResult.Fail("Danh sách phương tiện không được để trống.");

            var lanes = await _context.Lanes
                .Where(x => x.BranchId == branchId && x.IsActive)
                .OrderBy(x => x.IsBusinessLane ? 0 : 1) // business lane gets first pick
                .ToListAsync();

            if (!lanes.Any())
                return LaneScheduleResult.Fail("Không có làn xe khả dụng tại chi nhánh này.");

            var projectedFreeTimes = await GetLaneProjectedFreeTimesAsync(branchId, slotStart);

            // Local mutable state — we simulate without touching the DB
            var laneQueue = lanes
                .Select(l => new LaneSimState
                {
                    LaneId = l.LaneId,
                    IsBusinessLane = l.IsBusinessLane,
                    FreeAt = projectedFreeTimes.TryGetValue(l.LaneId, out var t)
                                        ? t
                                        : slotStart
                })
                .ToList();

            var assignments = new List<VehicleAssignment>();
            DateTime deadline = slotStart + slotDuration + TimeSpan.FromMinutes(SlotGraceMinutes);

            foreach (var vehicle in vehicles)
            {
                int washMinutes = WashTimeEstimator.EstimateMinutes(vehicle.ServicePrices);

                // Re-sort every iteration — FreeAt mutates as we assign
                laneQueue.Sort((a, b) => a.FreeAt.CompareTo(b.FreeAt));

                var chosenLane = laneQueue[0];

                // Vehicle cannot start before the slot opens
                DateTime estimatedStart = chosenLane.FreeAt < slotStart
                    ? slotStart
                    : chosenLane.FreeAt;

                DateTime estimatedEnd = estimatedStart.AddMinutes(washMinutes);

                if (estimatedEnd > deadline)
                {
                    return LaneScheduleResult.Fail(
                        $"Không đủ thời gian trong khung giờ cho {vehicles.Count} phương tiện. " +
                        $"Phương tiện thứ {assignments.Count + 1} ước tính hoàn thành lúc " +
                        $"{estimatedEnd:HH:mm}, vượt quá giới hạn cho phép ({deadline:HH:mm}).");
                }

                assignments.Add(new VehicleAssignment
                {
                    FleetVehicleId = vehicle.FleetVehicleId,
                    LaneId = chosenLane.LaneId,
                    EstimatedStart = estimatedStart,
                    EstimatedEnd = estimatedEnd
                });

                // Advance this lane's free time + inter-vehicle buffer
                chosenLane.FreeAt = estimatedEnd.AddMinutes(WashTimeEstimator.GetInterVehicleBuffer());
            }

            return LaneScheduleResult.Ok(assignments);
        }
    }
}