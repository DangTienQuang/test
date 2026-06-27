using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/operation-staff")]
    [Authorize(Roles = "Staff")]
    public class OperationStaffController : ControllerBase
    {
        private readonly IOperationStaffService _staffService;

        public OperationStaffController(IOperationStaffService staffService)
        {
            _staffService = staffService;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        [HttpPost("swap-lane")]
        public async Task<IActionResult> SwapLaneAssignment([FromBody] SwapLaneByPhoneDTO dto)
        {
            await _staffService.SwapLaneAssignmentByPhoneAsync(GetUserId(), dto);
            return Ok(new { Message = "Đổi ca/làn thành công." });
        }

        [HttpGet("lane-assignment")]
        public async Task<IActionResult> GetTodayLaneAssignment([FromQuery] System.DateTime? date)
        {
            var assignment = await _staffService.GetTodayLaneAssignmentAsync(GetUserId(), date);
            if (assignment == null)
            {
                return Ok(new { Message = "No lane assigned for the selected date." });
            }
            return Ok(assignment);
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetAssignedTasks([FromQuery] System.DateTime? date)
        {
            var tasks = await _staffService.GetAssignedBookingsAsync(GetUserId(), date);
            return Ok(tasks);
        }
        [HttpPost("bookings/{bookingId}/checkin")]
        public async Task<IActionResult> StaffCheckin(int bookingId)
        {
            await _staffService.CheckInBookingAsync(GetUserId(), bookingId);
            return Ok(new { Message = "Check-in xe thành công và đã phân vào làn của bạn." });
        }
        [HttpPut("bookings/{bookingId}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromBody] UpdateBookingStatusDTO dto)
        {
            await _staffService.UpdateBookingStatusAsync(GetUserId(), bookingId, dto.Status);
            return Ok(new { Message = $"Booking status updated to {dto.Status}." });
        }
    }
}
