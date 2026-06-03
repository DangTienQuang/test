using AutoWashPro.BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface IOperationStaffService
    {
        Task<StaffLaneTaskDTO?> GetTodayLaneAssignmentAsync(int staffUserId);
        Task<List<StaffBookingDetailDTO>> GetAssignedBookingDetailsAsync(int staffUserId);
        Task<bool> UpdateBookingDetailStatusAsync(int staffUserId, int detailId, string newStatus);
    }
}
