using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/manager")]
    [ApiController]
    [Authorize(Roles = "Manager,Admin")]
    public class ManagerStaffController : ControllerBase
    {
        private readonly IStaffManagementService _staffService;

        public ManagerStaffController(IStaffManagementService staffService)
        {
            _staffService = staffService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet("work-shifts")]
        public async Task<IActionResult> GetWorkShifts([FromQuery] bool includeInactive = false)
        {
            var result = await _staffService.GetWorkShiftsAsync(includeInactive);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("work-shifts")]
        public async Task<IActionResult> CreateWorkShift([FromBody] CreateWorkShiftDTO request)
        {
            var result = await _staffService.CreateWorkShiftAsync(request);
            return Created("", new { statusCode = 201, message = "Tao ca lam viec thanh cong.", data = result });
        }

        [HttpPut("work-shifts/{id}")]
        public async Task<IActionResult> UpdateWorkShift(int id, [FromBody] UpdateWorkShiftDTO request)
        {
            var result = await _staffService.UpdateWorkShiftAsync(id, request);
            return Ok(new { statusCode = 200, message = "Cap nhat ca lam viec thanh cong.", data = result });
        }

        [HttpDelete("work-shifts/{id}")]
        public async Task<IActionResult> DeleteWorkShift(int id)
        {
            await _staffService.DeleteWorkShiftAsync(id);
            return Ok(new { statusCode = 200, message = "Xoa hoac ngung su dung ca lam viec thanh cong." });
        }

        [HttpGet("shift-assignments")]
        public async Task<IActionResult> GetShiftAssignments([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null, [FromQuery] int? staffUserId = null)
        {
            var result = await _staffService.GetShiftAssignmentsAsync(fromDate, toDate, staffUserId);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("shift-assignments")]
        public async Task<IActionResult> CreateShiftAssignment([FromBody] CreateShiftAssignmentDTO request)
        {
            var result = await _staffService.CreateShiftAssignmentAsync(request);
            return Created("", new { statusCode = 201, message = "Phan cong ca thanh cong.", data = result });
        }

        [HttpPut("shift-assignments/{id}")]
        public async Task<IActionResult> UpdateShiftAssignment(int id, [FromBody] UpdateShiftAssignmentDTO request)
        {
            var result = await _staffService.UpdateShiftAssignmentAsync(id, request);
            return Ok(new { statusCode = 200, message = "Cap nhat phan cong ca thanh cong.", data = result });
        }

        [HttpDelete("shift-assignments/{id}")]
        public async Task<IActionResult> DeleteShiftAssignment(int id)
        {
            await _staffService.DeleteShiftAssignmentAsync(id);
            return Ok(new { statusCode = 200, message = "Xoa phan cong ca thanh cong." });
        }

        [HttpGet("overtime-requests")]
        public async Task<IActionResult> GetOvertimeRequests([FromQuery] string? status = null)
        {
            var result = await _staffService.GetOvertimeRequestsAsync(status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPut("overtime-requests/{id}/review")]
        public async Task<IActionResult> ReviewOvertimeRequest(int id, [FromBody] ReviewRequestDTO request)
        {
            var result = await _staffService.ReviewOvertimeRequestAsync(id, GetUserId(), request);
            return Ok(new { statusCode = 200, message = "Duyet yeu cau tang ca thanh cong.", data = result });
        }

        [HttpGet("shift-swap-requests")]
        public async Task<IActionResult> GetShiftSwapRequests([FromQuery] string? status = null)
        {
            var result = await _staffService.GetShiftSwapRequestsAsync(status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPut("shift-swap-requests/{id}/review")]
        public async Task<IActionResult> ReviewShiftSwapRequest(int id, [FromBody] ReviewRequestDTO request)
        {
            var result = await _staffService.ReviewShiftSwapRequestAsync(id, GetUserId(), request);
            return Ok(new { statusCode = 200, message = "Duyet yeu cau doi ca thanh cong.", data = result });
        }
    }
}
