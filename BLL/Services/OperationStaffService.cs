using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Exceptions;
using BLL.Helpers;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class OperationStaffService : IOperationStaffService
    {
        private readonly AutoWashDbContext _context;
        private readonly IWalletService _walletService;

        public OperationStaffService(AutoWashDbContext context, IWalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        public async Task<StaffLaneTaskDTO?> GetTodayLaneAssignmentAsync(int staffUserId, System.DateTime? date = null)
        {
            var targetDate = date?.Date ?? DateTime.UtcNow.ToVnTime().Date;

            var assignment = await _context.StaffLaneAssignments
                .Include(a => a.Lane)
                .Where(a => a.StaffId == staffUserId && a.AssignedDate.Date == targetDate)
                .OrderByDescending(a => a.AssignmentId)
                .FirstOrDefaultAsync();

            if (assignment == null) return null;

            return new StaffLaneTaskDTO
            {
                LaneId = assignment.LaneId,
                LaneName = assignment.Lane.Name,
                AssignedDate = assignment.AssignedDate
            };
        }

        public async Task<bool> SwapLaneAssignmentByPhoneAsync(int staffUserId, SwapLaneByPhoneDTO dto)
        {
            var targetDate = dto.Date?.Date ?? DateTime.UtcNow.ToVnTime().Date;

            // Find current user's assignment
            var currentAssignment = await _context.StaffLaneAssignments
                .Include(a => a.Staff)
                    .ThenInclude(s => s.EmployeeProfile)
                .FirstOrDefaultAsync(a => a.StaffId == staffUserId && a.AssignedDate.Date == targetDate);

            if (currentAssignment == null)
            {
                throw new BadRequestException("Bạn không có phân công làn nào trong ngày này để đổi.");
            }

            var branchId = currentAssignment.Staff.EmployeeProfile!.BranchId;

            // Find target staff by phone number
            var targetStaff = await _context.Users
                .Include(u => u.EmployeeProfile)
                .FirstOrDefaultAsync(u => u.PhoneNumber == dto.TargetStaffPhoneNumber && u.Role == "Staff");

            if (targetStaff == null || targetStaff.EmployeeProfile?.BranchId != branchId)
            {
                throw new NotFoundException("Staff with this phone number not found in your branch.");
            }

            // Find target user's assignment
            var targetAssignment = await _context.StaffLaneAssignments
                .FirstOrDefaultAsync(a => a.StaffId == targetStaff.UserId && a.AssignedDate.Date == targetDate);

            if (targetAssignment == null)
            {
                throw new BadRequestException("Nhân viên này không có phân công làn nào trong ngày này để đổi.");
            }

            // Swap LaneIds
            int tempLaneId = currentAssignment.LaneId;
            currentAssignment.LaneId = targetAssignment.LaneId;
            targetAssignment.LaneId = tempLaneId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckInBookingAsync(int staffUserId, int bookingId)
        {
            var today = System.DateTime.UtcNow.ToVnTime().Date;
            var assignment = await _context.StaffLaneAssignments
                .Where(a => a.StaffId == staffUserId && a.AssignedDate == today)
                .OrderByDescending(a => a.AssignmentId)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                throw new BadRequestException("Bạn chưa được phân công vào làn nào trong hôm nay. Không thể check-in.");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null)
            {
                throw new NotFoundException("Không tìm thấy thông tin đặt lịch.");
            }

            if (booking.Status != "Pending")
            {
                throw new BadRequestException("Chỉ có thể check-in cho xe đang ở trạng thái chờ (Pending).");
            }

            booking.ProcessingLaneId = assignment.LaneId;
            booking.ProcessingStaffId = staffUserId;
            booking.Status = "CheckedIn";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<StaffBookingDTO>> GetAssignedBookingsAsync(int staffUserId, System.DateTime? date = null)
        {
            var targetDate = date?.Date ?? DateTime.UtcNow.ToVnTime().Date;

            var assignment = await _context.StaffLaneAssignments
                .Where(a => a.StaffId == staffUserId && a.AssignedDate.Date == targetDate)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return new List<StaffBookingDTO>();
            }

            var bookings = await _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Service)
                .Include(b => b.ActualVehicleType)
                .Where(b => b.ProcessingLaneId == assignment.LaneId
                         && b.ScheduledTime.Date == targetDate
                         && (b.ProcessingStaffId == staffUserId || b.ProcessingStaffId == null)
                         && (b.Status == "CheckedIn" || b.Status == "Processing" || b.Status == "Pending"))
                .ToListAsync();

            return bookings.Select(b => new StaffBookingDTO
            {
                BookingId = b.BookingId,
                LicensePlate = b.LicensePlate,
                ServiceNames = b.BookingDetails.Select(d => d.Service.ServiceName).ToList(),
                VehicleTypeName = b.ActualVehicleType?.Name ?? "Unknown",
                Status = b.Status
            }).ToList();
        }

        public async Task<bool> UpdateBookingStatusAsync(int staffUserId, int bookingId, string newStatus)
        {
            if (newStatus != "Processing" && newStatus != "Completed")
            {
                throw new BadRequestException("Invalid status update.");
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) throw new NotFoundException("Booking not found.");
            if (newStatus == "Processing")
            {
                 if (booking.Status != "CheckedIn" && booking.Status != "Processing")
                     throw new BadRequestException("Can only start processing checked-in vehicles.");
                 booking.ProcessingStaffId = staffUserId;
            }

            if (newStatus == "Completed")
            {
                if (booking.Status != "Processing" && booking.Status != "Completed")
                    throw new BadRequestException("Can only complete processing vehicles.");

                if (booking.ProcessingStaffId != staffUserId)
                    throw new BadRequestException("You are not assigned to this vehicle.");
            }

            booking.Status = newStatus;

            if (newStatus == "Completed" && booking.UserId > 0)
            {
                 var userProfile = await _context.CustomerProfiles
                        .Include(cp => cp.Tier)
                        .FirstOrDefaultAsync(cp => cp.UserId == booking.UserId);

                 if (userProfile?.Tier != null && booking.FinalAmount > 0)
                 {
                        int pointsEarned = (int)((booking.FinalAmount / PointConstants.VndPerEarnedPoint) * (decimal)userProfile.Tier.PointMultiplier);

                        if (pointsEarned > 0)
                        {
                            await _walletService.AwardCompletionPointsAsync(
                                booking.UserId.Value, pointsEarned, booking.BookingId);
                        }
                 }

                 if (userProfile != null)
                 {
                     userProfile.LastVisitDate = DateTime.UtcNow;
                 }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
