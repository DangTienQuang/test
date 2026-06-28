using BLL.DTOs.Fleet;
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

        [HttpGet("staff/pending/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPendingVehicles()
        {
            var result = await _fleetService.GetAllPendingVehiclesAsync();
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("staff/approve/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveFleetVehicle(int id)
        {
            await _fleetService.ApproveFleetVehicleAsync(id);
            return Ok(new { statusCode = 200, message = "Đã phê duyệt xe." });
        }

        [HttpPost("staff/reject/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectFleetVehicle(int id, [FromBody] RejectFleetVehicleDTO request)
        {
            await _fleetService.RejectFleetVehicleAsync(id, request.Reason);
            return Ok(new { statusCode = 200, message = "Đã từ chối xe." });
        }
    }
}
