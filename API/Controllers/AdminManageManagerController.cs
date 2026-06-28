using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/admin/managers")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminManageManagerController : ControllerBase
    {
        private readonly IStaffManagementService _staffService;

        public AdminManageManagerController(IStaffManagementService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetManagers([FromQuery] string? keyword = null, [FromQuery] string? status = null)
        {
            var result = await _staffService.GetStaffsByRoleAsync(UserRoles.Manager, keyword, status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetManagerDetail(int id)
        {
            var result = await _staffService.GetStaffByRoleAsync(id, UserRoles.Manager);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost]
        public async Task<IActionResult> CreateManager([FromBody] CreateStaffDTO request)
        {
            var result = await _staffService.CreateStaffWithRoleAsync(request, UserRoles.Manager);
            return Created("", new { statusCode = 201, message = "Tao manager thanh cong.", data = result });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateManager(int id, [FromBody] UpdateStaffDTO request)
        {
            var result = await _staffService.UpdateStaffByRoleAsync(id, UserRoles.Manager, request);
            return Ok(new { statusCode = 200, message = "Cap nhat manager thanh cong.", data = result });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateManagerStatus(int id, [FromBody] UpdateUserStatusDTO request)
        {
            await _staffService.GetStaffByRoleAsync(id, UserRoles.Manager);
            await _staffService.UpdateStaffStatusAsync(id, request.Status);
            return Ok(new { statusCode = 200, message = "Cap nhat trang thai manager thanh cong." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManager(int id)
        {
            await _staffService.SoftDeleteStaffByRoleAsync(id, UserRoles.Manager);
            return Ok(new { statusCode = 200, message = "Xoa manager thanh cong (soft delete)." });
        }
    }
}
