using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/admin/staffs")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminManageStaffController : ControllerBase
    {
        private readonly IStaffManagementService _staffService;

        public AdminManageStaffController(IStaffManagementService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStaffs([FromQuery] string? keyword = null, [FromQuery] string? status = null)
        {
            var result = await _staffService.GetStaffsByRoleAsync(UserRoles.Staff, keyword, status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStaffDetail(int id)
        {
            var result = await _staffService.GetStaffByRoleAsync(id, UserRoles.Staff);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDTO request)
        {
            var result = await _staffService.CreateStaffWithRoleAsync(request, UserRoles.Staff);
            return Created("", new { statusCode = 201, message = "Tao staff thanh cong.", data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromBody] UpdateStaffDTO request)
        {
            var result = await _staffService.UpdateStaffByRoleAsync(id, UserRoles.Staff, request);
            return Ok(new { statusCode = 200, message = "Cap nhat staff thanh cong.", data = result });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStaffStatus(int id, [FromBody] UpdateUserStatusDTO request)
        {
            await _staffService.GetStaffByRoleAsync(id, UserRoles.Staff);
            await _staffService.UpdateStaffStatusAsync(id, request.Status);
            return Ok(new { statusCode = 200, message = "Cap nhat trang thai staff thanh cong." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            await _staffService.SoftDeleteStaffByRoleAsync(id, UserRoles.Staff);
            return Ok(new { statusCode = 200, message = "Xoa staff thanh cong (soft delete)." });
        }
    }
}
