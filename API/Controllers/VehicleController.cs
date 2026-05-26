using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/vehicles")]
    [ApiController]
    [Authorize]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyVehicles()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _vehicleService.GetMyVehiclesAsync(userId);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost]
        public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDTO request)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _vehicleService.AddVehicleAsync(userId, request);
            return Created("", new { statusCode = 201, message = "Thêm xe thành công." });
        }

        [HttpPut("{licensePlate}")]
        public async Task<IActionResult> UpdateVehicle(string licensePlate, [FromBody] UpdateVehicleDTO request)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _vehicleService.UpdateVehicleAsync(userId, licensePlate, request);
            return Ok(new { statusCode = 200, message = "Cập nhật thông tin xe thành công." });
        }

        [HttpDelete("{licensePlate}")]
        public async Task<IActionResult> DeleteVehicle(string licensePlate)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            await _vehicleService.DeleteVehicleAsync(userId, licensePlate);
            return Ok(new { statusCode = 200, message = "Đã xóa phương tiện khỏi hồ sơ." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("recognize/{licensePlate}")]
        public async Task<IActionResult> RecognizeVehicle(string licensePlate)
        {
            var result = await _vehicleService.RecognizeVehicleAsync(licensePlate);
            return Ok(new { statusCode = 200, message = "Nhận diện thành công", data = result });
        }
    }
}