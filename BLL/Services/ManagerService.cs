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
                .Include(b => b.ProcessingLane)
                .Include(b => b.ProcessingStaff)
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
                LicensePlate = b.LicensePlate,
                ServiceNames = b.BookingDetails.Select(d => d.Service.ServiceName).ToList(),
                ProcessingLaneId = b.ProcessingLaneId,
                ProcessingLaneName = b.ProcessingLane?.Name,
                ProcessingStaffId = b.ProcessingStaffId,
                ProcessingStaffName = b.ProcessingStaff?.EmployeeProfile?.FullName
            }).ToList();
        }

        public async Task<bool> ConfirmCheckInAndAssignLaneAsync(int managerUserId, int bookingId, AssignBookingToLaneDTO assignment)
        {
            var managerProfile = await GetManagerProfileAsync(managerUserId);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.BranchId == managerProfile.BranchId);

            if (booking == null)
            {
                throw new NotFoundException("Booking not found in your branch.");
            }

            if (booking.Status != "Pending" && booking.Status != "CheckedIn")
            {
                throw new BadRequestException("Booking is not in a valid state for check-in and assignment.");
            }

            var validLane = await _context.Lanes
                .AnyAsync(l => l.LaneId == assignment.LaneId && l.BranchId == managerProfile.BranchId);

            if (!validLane)
            {
                throw new BadRequestException("Làn (Lane) không hợp lệ hoặc không thuộc chi nhánh của bạn.");
            }

            booking.ProcessingLaneId = assignment.LaneId;
            booking.Status = "CheckedIn";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
