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
        public async Task<IActionResult> AddVehicle([FromForm] CreateVehicleRequest request)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var dto = new CreateVehicleDTO
            {
                LicensePlate = request.LicensePlate,
                VehicleTypeId = request.VehicleTypeId,
                RegistrationPhotoUrl = request.RegistrationPhotoUrl,
                UserNote = request.UserNote,
                CarModelId = request.CarModelId,
                CarModel = request.CarModel
            };

            if (request.PhotoFile != null)
            {
                dto.PhotoStream = request.PhotoFile.OpenReadStream();
                dto.PhotoFileName = request.PhotoFile.FileName;
            }

            await _vehicleService.AddVehicleAsync(userId, dto);
            return Created("", new { statusCode = 201, message = "Thêm xe thành công." });
        }

        [HttpPut("{licensePlate}")]
        public async Task<IActionResult> UpdateVehicle(string licensePlate, [FromForm] UpdateVehicleRequest request)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var dto = new UpdateVehicleDTO
            {
                VehicleTypeId = request.VehicleTypeId,
                UserNote = request.UserNote,
                CarModelId = request.CarModelId,
                CarModel = request.CarModel
            };

            if (request.PhotoFile != null)
            {
                dto.PhotoStream = request.PhotoFile.OpenReadStream();
                dto.PhotoFileName = request.PhotoFile.FileName;
            }

            await _vehicleService.UpdateVehicleAsync(userId, licensePlate, dto);
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