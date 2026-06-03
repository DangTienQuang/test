using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/manager")]
    [Authorize(Roles = "Manager")]
    public class ManagerController : ControllerBase
    {
        private readonly IManagerService _managerService;

        public ManagerController(IManagerService managerService)
        {
            _managerService = managerService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        [HttpGet("staff")]
        public async Task<IActionResult> GetStaffInBranch()
        {
            var staffList = await _managerService.GetStaffInBranchAsync(GetUserId());
            return Ok(staffList);
        }

        [HttpPost("lanes/assign-staff")]
        public async Task<IActionResult> AssignStaffToLane([FromBody] AssignStaffToLaneDTO dto)
        {
            await _managerService.AssignStaffToLaneAsync(GetUserId(), dto);
            return Ok(new { Message = "Staff assigned to lane successfully." });
        }

        [HttpGet("bookings")]
        public async Task<IActionResult> GetCheckInBookings()
        {
            var bookings = await _managerService.GetCheckInBookingsInBranchAsync(GetUserId());
            return Ok(bookings);
        }

        [HttpPost("bookings/{bookingId}/checkin-assign")]
        public async Task<IActionResult> ConfirmCheckInAndAssignLane(int bookingId, [FromBody] List<AssignBookingDetailDTO> assignments)
        {
            await _managerService.ConfirmCheckInAndAssignLaneAsync(GetUserId(), bookingId, assignments);
            return Ok(new { Message = "Booking checked in and lanes assigned successfully." });
        }
    }
}
