using AutoWashPro.BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface IOperationStaffService
    {
        Task<StaffLaneTaskDTO?> GetTodayLaneAssignmentAsync(int staffUserId);
        Task<List<StaffBookingDTO>> GetAssignedBookingsAsync(int staffUserId);
        Task<bool> UpdateBookingStatusAsync(int staffUserId, int bookingId, string newStatus);
        Task<bool> CheckInBookingAsync(int staffUserId, int bookingId);
    }
}
