using AutoWashPro.BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface IManagerService
    {
        Task<List<ManagerStaffDTO>> GetStaffInBranchAsync(int managerUserId);
        Task<bool> AssignStaffToLaneAsync(int managerUserId, AssignStaffToLaneDTO assignDto);
        Task<List<ManagerBookingListDTO>> GetCheckInBookingsInBranchAsync(int managerUserId);
        Task<bool> ConfirmCheckInAndAssignLaneAsync(int managerUserId, int bookingId, AssignBookingToLaneDTO assignment);
    }
}
