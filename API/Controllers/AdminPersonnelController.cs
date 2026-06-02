using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminPersonnelController : ControllerBase
    {
        private readonly IStaffManagementService _staffService;

        public AdminPersonnelController(IStaffManagementService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet("staffs")]
        public async Task<IActionResult> GetStaffs([FromQuery] string? keyword = null, [FromQuery] string? status = null)
        {
            var result = await _staffService.GetStaffsByRoleAsync(UserRoles.Staff, keyword, status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpGet("staffs/{id}")]
        public async Task<IActionResult> GetStaffDetail(int id)
        {
            var result = await _staffService.GetStaffByRoleAsync(id, UserRoles.Staff);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("staffs")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDTO request)
        {
            var result = await _staffService.CreateStaffWithRoleAsync(request, UserRoles.Staff);
            return Created("", new { statusCode = 201, message = "Tao staff thanh cong.", data = result });
        }

        [HttpPut("staffs/{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromBody] UpdateStaffDTO request)
        {
            var result = await _staffService.UpdateStaffByRoleAsync(id, UserRoles.Staff, request);
            return Ok(new { statusCode = 200, message = "Cap nhat staff thanh cong.", data = result });
        }

        [HttpPut("staffs/{id}/status")]
        public async Task<IActionResult> UpdateStaffStatus(int id, [FromBody] UpdateUserStatusDTO request)
        {
            await _staffService.GetStaffByRoleAsync(id, UserRoles.Staff);
            await _staffService.UpdateStaffStatusAsync(id, request.Status);
            return Ok(new { statusCode = 200, message = "Cap nhat trang thai staff thanh cong." });
        }

        [HttpDelete("staffs/{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            await _staffService.SoftDeleteStaffByRoleAsync(id, UserRoles.Staff);
            return Ok(new { statusCode = 200, message = "Xoa staff thanh cong (soft delete)." });
        }

        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers([FromQuery] string? keyword = null, [FromQuery] string? status = null)
        {
            var result = await _staffService.GetStaffsByRoleAsync(UserRoles.Manager, keyword, status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpGet("managers/{id}")]
        public async Task<IActionResult> GetManagerDetail(int id)
        {
            var result = await _staffService.GetStaffByRoleAsync(id, UserRoles.Manager);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("managers")]
        public async Task<IActionResult> CreateManager([FromBody] CreateStaffDTO request)
        {
            var result = await _staffService.CreateStaffWithRoleAsync(request, UserRoles.Manager);
            return Created("", new { statusCode = 201, message = "Tao manager thanh cong.", data = result });
        }

        [HttpPut("managers/{id}")]
        public async Task<IActionResult> UpdateManager(int id, [FromBody] UpdateStaffDTO request)
        {
            var result = await _staffService.UpdateStaffByRoleAsync(id, UserRoles.Manager, request);
            return Ok(new { statusCode = 200, message = "Cap nhat manager thanh cong.", data = result });
        }

        [HttpPut("managers/{id}/status")]
        public async Task<IActionResult> UpdateManagerStatus(int id, [FromBody] UpdateUserStatusDTO request)
        {
            await _staffService.GetStaffByRoleAsync(id, UserRoles.Manager);
            await _staffService.UpdateStaffStatusAsync(id, request.Status);
            return Ok(new { statusCode = 200, message = "Cap nhat trang thai manager thanh cong." });
        }

        [HttpDelete("managers/{id}")]
        public async Task<IActionResult> DeleteManager(int id)
        {
            await _staffService.SoftDeleteStaffByRoleAsync(id, UserRoles.Manager);
            return Ok(new { statusCode = 200, message = "Xoa manager thanh cong (soft delete)." });
        }
    }
}
