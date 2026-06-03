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

        public OperationStaffService(AutoWashDbContext context)
        {
            _context = context;
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

        public async Task<List<StaffBookingDetailDTO>> GetAssignedBookingDetailsAsync(int staffUserId)
        {
            var today = DateTime.UtcNow.Date;

            // Find lane assignment for today
            var assignment = await _context.StaffLaneAssignments
                .Where(a => a.StaffId == staffUserId && a.AssignedDate == today)
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return new List<StaffBookingDetailDTO>();
            }

            // Find all booking details assigned to this lane and staff
            var details = await _context.BookingDetails
                .Include(d => d.Booking)
                .Include(d => d.Service)
                .Include(d => d.ActualVehicleType)
                .Where(d => d.ProcessingLaneId == assignment.LaneId
                         && d.ProcessingStaffId == staffUserId
                         && d.Booking.ScheduledTime.Date == today
                         && (d.Booking.Status == "CheckedIn" || d.Booking.Status == "Processing"))
                .ToListAsync();

            return details.Select(d => new StaffBookingDetailDTO
            {
                DetailId = d.DetailId,
                BookingId = d.BookingId,
                LicensePlate = d.LicensePlate,
                ServiceName = d.Service.ServiceName,
                VehicleTypeName = d.ActualVehicleType?.Name ?? "Unknown",
                Status = d.Booking.Status
            }).ToList();
        }

        public async Task<bool> UpdateBookingStatusAsync(int staffUserId, int bookingId, string newStatus)
        {
            if (newStatus != "Processing" && newStatus != "Completed")
            {
                throw new BadRequestException("Invalid status update.");
            }

            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) throw new NotFoundException("Booking not found.");

            // Verify if staff is assigned to at least one detail of this booking
            bool isAssigned = booking.BookingDetails.Any(d => d.ProcessingStaffId == staffUserId);
            if (!isAssigned)
            {
                throw new BadRequestException("You are not assigned to this booking.");
            }

            if (newStatus == "Processing" && booking.Status != "CheckedIn" && booking.Status != "Processing")
            {
                throw new BadRequestException("Can only start processing checked-in bookings.");
            }

            if (newStatus == "Completed" && booking.Status != "Processing" && booking.Status != "Completed")
            {
                throw new BadRequestException("Can only complete processing bookings.");
            }

            booking.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
