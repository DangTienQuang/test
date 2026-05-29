using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/admin/vehicles")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminVehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public AdminVehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet("other-types")]
        public async Task<IActionResult> GetOtherVehicles()
        {
            var result = await _vehicleService.GetOtherVehiclesAsync();
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPut("{licensePlate}/type")]
        public async Task<IActionResult> UpdateVehicleType(string licensePlate, [FromBody] UpdateVehicleTypeAdminDTO request)
        {
            await _vehicleService.UpdateVehicleTypeByAdminAsync(licensePlate, request.VehicleTypeId);
            return Ok(new { statusCode = 200, message = "Cập nhật loại xe thành công." });
        }
    }
}
