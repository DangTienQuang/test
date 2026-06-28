using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers.Admin
{
    [Route("api/v1/admin/employees")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminEmployeeController : ControllerBase
    {
        private readonly IStaffManagementService _staffService;
        private readonly IEmployeeService _employeeService;

        public AdminEmployeeController(IStaffManagementService staffService, IEmployeeService employeeService)
        {
            _staffService = staffService;
            _employeeService = employeeService;
        }

        private string MapRoleType(string roleType)
        {
            return roleType.ToLower() switch
            {
                "staff" => UserRoles.Staff,
                "staffs" => UserRoles.Staff,
                "manager" => UserRoles.Manager,
                "managers" => UserRoles.Manager,
                _ => throw new ArgumentException("Invalid role type. Allowed values are 'staff' or 'manager'.")
            };
        }

        [HttpGet("{roleType}")]
        public async Task<IActionResult> GetEmployees(string roleType, [FromQuery] string? keyword = null, [FromQuery] string? status = null)
        {
            var role = MapRoleType(roleType);
            var result = await _staffService.GetStaffsByRoleAsync(role, keyword, status);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpGet("{roleType}/{id}")]
        public async Task<IActionResult> GetEmployeeDetail(string roleType, int id)
        {
            var role = MapRoleType(roleType);
            var result = await _staffService.GetStaffByRoleAsync(id, role);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("{roleType}")]
        public async Task<IActionResult> CreateEmployee(string roleType, [FromBody] CreateStaffDTO request)
        {
            var role = MapRoleType(roleType);
            var result = await _staffService.CreateStaffWithRoleAsync(request, role);
            return Created("", new { statusCode = 201, message = $"Tao {roleType} thanh cong.", data = result });
        }

        [HttpPut("{roleType}/{id}")]
        public async Task<IActionResult> UpdateEmployee(string roleType, int id, [FromBody] UpdateStaffDTO request)
        {
            var role = MapRoleType(roleType);
            var result = await _staffService.UpdateStaffByRoleAsync(id, role, request);
            return Ok(new { statusCode = 200, message = $"Cap nhat {roleType} thanh cong.", data = result });
        }

        [HttpPut("{roleType}/{id}/status")]
        public async Task<IActionResult> UpdateEmployeeStatus(string roleType, int id, [FromBody] UpdateUserStatusDTO request)
        {
            var role = MapRoleType(roleType);
            await _staffService.GetStaffByRoleAsync(id, role);
            await _staffService.UpdateStaffStatusAsync(id, request.Status);
            return Ok(new { statusCode = 200, message = $"Cap nhat trang thai {roleType} thanh cong." });
        }

        [HttpDelete("{roleType}/{id}")]
        public async Task<IActionResult> DeleteEmployee(string roleType, int id)
        {
            var role = MapRoleType(roleType);
            await _staffService.SoftDeleteStaffByRoleAsync(id, role);
            return Ok(new { statusCode = 200, message = $"Xoa {roleType} thanh cong (soft delete)." });
        }

        [HttpPut("{id}/transfer")]
        public async Task<IActionResult> TransferEmployee(int id, [FromBody] TransferEmployeeDTO dto)
        {
            var result = await _employeeService.TransferEmployeeAsync(id, dto);
            return Ok(new { Success = result });
        }
    }
}
