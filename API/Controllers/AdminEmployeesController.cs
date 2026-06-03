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
    public class AdminEmployeesController : ControllerBase
    {
        private readonly IStaffManagementService _staffService;

        public AdminEmployeesController(IStaffManagementService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees([FromQuery] string? role = null, [FromQuery] string? keyword = null, [FromQuery] string? status = null)
        {
            var result = await _staffService.GetEmployeesAsync(role, keyword, status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpGet("employees/{id}")]
        public async Task<IActionResult> GetEmployeeDetail(int id)
        {
            var result = await _staffService.GetEmployeeAsync(id);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("employees")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDTO request)
        {
            var result = await _staffService.CreateEmployeeAsync(request);
            return Created("", new { statusCode = 201, message = "Tạo nhân viên thành công.", data = result });
        }

        [HttpPut("employees/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDTO request)
        {
            var result = await _staffService.UpdateEmployeeAsync(id, request);
            return Ok(new { statusCode = 200, message = "Cập nhật nhân viên thành công.", data = result });
        }

        [HttpPut("employees/{id}/status")]
        public async Task<IActionResult> UpdateEmployeeStatus(int id, [FromBody] UpdateUserStatusDTO request)
        {
            await _staffService.GetEmployeeAsync(id);
            await _staffService.UpdateEmployeeStatusAsync(id, request.Status);
            return Ok(new { statusCode = 200, message = "Cập nhật trạng thái nhân viên thành công." });
        }

        [HttpDelete("employees/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            await _staffService.GetEmployeeAsync(id); // Ensure exists before deleting
            await _staffService.SoftDeleteEmployeeAsync(id);
            return Ok(new { statusCode = 200, message = "Xóa nhân viên thành công (soft delete)." });
        }
    }
}
