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
                         && (d.Status == "CheckedIn" || d.Status == "Processing"))
                .ToListAsync();

            return details.Select(d => new StaffBookingDetailDTO
            {
                DetailId = d.DetailId,
                BookingId = d.BookingId,
                LicensePlate = d.LicensePlate,
                ServiceName = d.Service.ServiceName,
                VehicleTypeName = d.ActualVehicleType?.Name ?? "Unknown",
                Status = d.Status
            }).ToList();
        }

        public async Task<bool> UpdateBookingDetailStatusAsync(int staffUserId, int detailId, string newStatus)
        {
            if (newStatus != "Processing" && newStatus != "Completed")
            {
                throw new BadRequestException("Invalid status update.");
            }

            var detail = await _context.BookingDetails
                .Include(d => d.Booking)
                .FirstOrDefaultAsync(d => d.DetailId == detailId);

            if (detail == null) throw new NotFoundException("Booking detail not found.");

            if (detail.ProcessingStaffId != staffUserId)
            {
                throw new BadRequestException("You are not assigned to this vehicle.");
            }

            if (newStatus == "Processing" && detail.Status != "CheckedIn" && detail.Status != "Processing")
            {
                throw new BadRequestException("Can only start processing checked-in vehicles.");
            }

            if (newStatus == "Completed" && detail.Status != "Processing" && detail.Status != "Completed")
            {
                throw new BadRequestException("Can only complete processing vehicles.");
            }

            detail.Status = newStatus;

            // Check if all details in the booking are completed
            if (newStatus == "Completed")
            {
                var allDetails = await _context.BookingDetails.Where(d => d.BookingId == detail.BookingId).ToListAsync();
                if (allDetails.All(d => d.Status == "Completed" || (d.DetailId == detailId)))
                {
                    detail.Booking.Status = "Completed";
                }
                else
                {
                    detail.Booking.Status = "Processing"; // At least one car is completed but not all
                }
            }
            else if (newStatus == "Processing" && detail.Booking.Status == "CheckedIn")
            {
                 detail.Booking.Status = "Processing";
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
