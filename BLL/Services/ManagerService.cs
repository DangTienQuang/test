using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Exceptions;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class ManagerService : IManagerService
    {
        private readonly AutoWashDbContext _context;

        public ManagerService(AutoWashDbContext context)
        {
            _context = context;
        }

        private async Task<EmployeeProfile> GetManagerProfileAsync(int managerUserId)
        {
            var profile = await _context.EmployeeProfiles
                .FirstOrDefaultAsync(e => e.EmployeeId == managerUserId);

            if (profile == null)
            {
                throw new BadRequestException("Manager profile not found.");
            }

            if (!profile.BranchId.HasValue)
            {
                throw new BadRequestException("Manager is not assigned to any branch.");
            }

            return profile;
        }

        public async Task<List<ManagerStaffDTO>> GetStaffInBranchAsync(int managerUserId)
        {
            var managerProfile = await GetManagerProfileAsync(managerUserId);

            var staffList = await _context.EmployeeProfiles
                .Include(e => e.User)
                .Where(e => e.BranchId == managerProfile.BranchId && e.User.Role == "Staff")
                .Select(e => new ManagerStaffDTO
                {
                    UserId = e.EmployeeId,
                    FullName = e.FullName,
                    PhoneNumber = e.User.PhoneNumber,
                    Status = e.User.Status
                })
                .ToListAsync();

            return staffList;
        }

        public async Task<bool> AssignStaffToLaneAsync(int managerUserId, AssignStaffToLaneDTO assignDto)
        {
            var managerProfile = await GetManagerProfileAsync(managerUserId);

            // Verify staff belongs to manager's branch
            var staffProfile = await _context.EmployeeProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployeeId == assignDto.StaffId && e.BranchId == managerProfile.BranchId && e.User.Role == "Staff");

            if (staffProfile == null)
            {
                throw new BadRequestException("Staff not found in your branch.");
            }

            // Verify lane belongs to manager's branch
            var lane = await _context.Lanes.FirstOrDefaultAsync(l => l.LaneId == assignDto.LaneId && l.BranchId == managerProfile.BranchId);
            if (lane == null)
            {
                throw new BadRequestException("Lane not found in your branch.");
            }

            var assignment = new StaffLaneAssignment
            {
                StaffId = assignDto.StaffId,
                LaneId = assignDto.LaneId,
                AssignedDate = assignDto.AssignedDate.Date
            };

            _context.StaffLaneAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ManagerBookingListDTO>> GetCheckInBookingsInBranchAsync(int managerUserId)
        {
            var managerProfile = await GetManagerProfileAsync(managerUserId);

            var bookings = await _context.Bookings
                .Include(b => b.User)
                    .ThenInclude(u => u.CustomerProfile)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Service)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.ProcessingLane)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.ProcessingStaff)
                        .ThenInclude(s => s.EmployeeProfile)
                .Where(b => b.BranchId == managerProfile.BranchId && (b.Status == "CheckedIn" || b.Status == "Pending" || b.Status == "Processing"))
                .ToListAsync();

            return bookings.Select(b => new ManagerBookingListDTO
            {
                BookingId = b.BookingId,
                Status = b.Status,
                ScheduledTime = b.ScheduledTime,
                CustomerName = b.User?.CustomerProfile?.FullName,
                CustomerPhone = b.User?.PhoneNumber,
                Details = b.BookingDetails.Select(d => new ManagerBookingDetailDTO
                {
                    DetailId = d.DetailId,
                    LicensePlate = d.LicensePlate,
                    ServiceName = d.Service.ServiceName,
                    ProcessingLaneId = d.ProcessingLaneId,
                    ProcessingLaneName = d.ProcessingLane?.Name,
                    ProcessingStaffId = d.ProcessingStaffId,
                    ProcessingStaffName = d.ProcessingStaff?.EmployeeProfile?.FullName
                }).ToList()
            }).ToList();
        }

        public async Task<bool> ConfirmCheckInAndAssignLaneAsync(int managerUserId, int bookingId, List<AssignBookingDetailDTO> assignments)
        {
            var managerProfile = await GetManagerProfileAsync(managerUserId);

            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.BranchId == managerProfile.BranchId);

            if (booking == null)
            {
                throw new NotFoundException("Booking not found in your branch.");
            }

            if (booking.Status != "Pending" && booking.Status != "CheckedIn")
            {
                throw new BadRequestException("Booking is not in a valid state for check-in and assignment.");
            }

            var laneIds = assignments.Select(a => a.LaneId).Distinct().ToList();
            var staffIds = assignments.Select(a => a.StaffId).Distinct().ToList();

            var validLanes = await _context.Lanes
                .Where(l => laneIds.Contains(l.LaneId) && l.BranchId == managerProfile.BranchId)
                .Select(l => l.LaneId)
                .ToListAsync();

            var validStaff = await _context.EmployeeProfiles
                .Where(e => staffIds.Contains(e.EmployeeId) && e.BranchId == managerProfile.BranchId)
                .Select(e => e.EmployeeId)
                .ToListAsync();

            foreach (var detail in booking.BookingDetails)
            {
                var assignInfo = assignments.FirstOrDefault(a => a.BookingDetailId == detail.DetailId);
                if (assignInfo != null)
                {
                    if (!validLanes.Contains(assignInfo.LaneId) || !validStaff.Contains(assignInfo.StaffId))
                    {
                        throw new BadRequestException($"Invalid Lane or Staff for Detail {detail.DetailId}");
                    }

                    detail.ProcessingLaneId = assignInfo.LaneId;
                    detail.ProcessingStaffId = assignInfo.StaffId;
                }
            }

            // Update status
            booking.Status = "CheckedIn";
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
