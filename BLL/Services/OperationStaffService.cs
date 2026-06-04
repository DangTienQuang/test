using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Exceptions;
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

        public async Task<StaffLaneTaskDTO?> GetTodayLaneAssignmentAsync(int staffUserId)
        {
            var today = DateTime.UtcNow.Date; // Should use VN time normally, simplifying for now

            var assignment = await _context.StaffLaneAssignments
                .Include(a => a.Lane)
                .Where(a => a.StaffId == staffUserId && a.AssignedDate == today)
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

        public async Task<List<StaffBookingDTO>> GetAssignedBookingsAsync(int staffUserId)
        {
            var today = DateTime.UtcNow.Date;

            // Find lane assignment for today
            var assignment = await _context.StaffLaneAssignments
                .Where(a => a.StaffId == staffUserId && a.AssignedDate == today)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return new List<StaffBookingDTO>();
            }

            // Find all bookings assigned to this lane and staff
            var bookings = await _context.Bookings
                .Include(b => b.BookingDetails)
                .ThenInclude(d => d.Service)
                .Include(b => b.ActualVehicleType)
                .Where(b => b.ProcessingLaneId == assignment.LaneId
                         && (b.ProcessingStaffId == staffUserId || b.ProcessingStaffId == null) // Show checked-in cars assigned to lane, or cars already processing by this staff
                         && b.ScheduledTime.Date == today
                         && (b.Status == "CheckedIn" || b.Status == "Processing"))
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

            // Assign staff if they are starting the process
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

            // Trigger completion logic if applicable (e.g. points calculation)
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
