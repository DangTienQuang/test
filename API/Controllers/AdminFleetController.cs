using BLL.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [ApiController]
    [Route("api/v1/fleet/staff")]
    [Authorize(Roles = "Admin")]
    public class AdminFleetController : ControllerBase
    {
        private readonly IFleetService _fleetService;

        public AdminFleetController(IFleetService fleetService)
        {
            _fleetService = fleetService;
        }

        [HttpGet("pending/all")]
        public async Task<IActionResult> GetAllPendingVehicles()
        {
            var result = await _fleetService.GetAllPendingVehiclesAsync();
            return Ok(result);
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveFleetVehicle(int id)
        {
            await _fleetService.ApproveFleetVehicleAsync(id);
            return Ok(new { Message = "Phê duyệt phương tiện fleet thành công." });
        }

        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectFleetVehicle(int id, [FromQuery] string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new { Message = "Vui lòng cung cấp lý do từ chối." });
            }
            await _fleetService.RejectFleetVehicleAsync(id, reason);
            return Ok(new { Message = "Từ chối phương tiện fleet thành công." });
        }
    }
}
