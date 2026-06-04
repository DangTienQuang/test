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

        [HttpGet("lane-assignment")]
        public async Task<IActionResult> GetTodayLaneAssignment()
        {
            var assignment = await _staffService.GetTodayLaneAssignmentAsync(GetUserId());
            if (assignment == null)
            {
                return Ok(new { Message = "No lane assigned for today." });
            }
            return Ok(assignment);
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetAssignedTasks()
        {
            var tasks = await _staffService.GetAssignedBookingsAsync(GetUserId());
            return Ok(tasks);
        }

        [HttpPut("bookings/{bookingId}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, [FromBody] UpdateBookingStatusDTO dto)
        {
            await _staffService.UpdateBookingStatusAsync(GetUserId(), bookingId, dto.Status);
            return Ok(new { Message = $"Booking status updated to {dto.Status}." });
        }
    }
}
