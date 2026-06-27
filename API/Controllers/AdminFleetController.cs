using BLL.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/fleet")]
    [Authorize(Roles = "Admin")]
    public class AdminFleetController : ControllerBase
    {
        private readonly IFleetService _fleetService;

        public AdminFleetController(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        [HttpGet("vehicle/pending/all")]
        public async Task<IActionResult> GetAllPendingVehicles()
        {
            var result = await _fleetService.GetAllPendingVehiclesAsync();
            return Ok(result);
        }

        [HttpPost("vehicle/approve/{id}")]
        public async Task<IActionResult> ApproveFleetVehicle(int id)
        {
            await _fleetService.ApproveFleetVehicleAsync(id);
            return Ok(new { Message = "Phê duyệt phương tiện fleet thành công." });
        }

        [HttpPost("vehicle/reject/{id}")]
        public async Task<IActionResult> RejectFleetVehicle(int id, [FromQuery] string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new { Message = "Vui lòng cung cấp lý do từ chối." });
            }
            await _fleetService.RejectFleetVehicleAsync(id, reason);
            return Ok(new { Message = "Từ chối phương tiện fleet thành công." });
        }

        [HttpGet("staff/pending/all")]
        public async Task<IActionResult> GetAllPendingStaff()
        {
            var result = await _fleetService.GetAllPendingStaffAsync();
            return Ok(result);
        }

        [HttpPost("staff/approve/{id}")]
        public async Task<IActionResult> ApproveFleetStaff(int id)
        {
            await _fleetService.ApproveFleetStaffAsync(id);
            return Ok(new { Message = "Phê duyệt nhân viên fleet thành công." });
        }

        [HttpPost("staff/reject/{id}")]
        public async Task<IActionResult> RejectFleetStaff(int id, [FromQuery] string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new { Message = "Vui lòng cung cấp lý do từ chối." });
            }
            await _fleetService.RejectFleetStaffAsync(id, reason);
            return Ok(new { Message = "Từ chối nhân viên fleet thành công." });
        }
    }
}
