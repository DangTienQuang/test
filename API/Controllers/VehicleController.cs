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
        public async Task<IActionResult> GetVehicles()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                int userId = int.Parse(userIdClaim);
                var result = await _vehicleService.GetVehiclesAsync(userId);

                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                int userId = int.Parse(userIdClaim);
                await _vehicleService.AddVehicleAsync(userId, request);

                return Created("", new { statusCode = 201, message = "Vehicle added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicle(string id, [FromBody] UpdateVehicleDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                int userId = int.Parse(userIdClaim);
                await _vehicleService.UpdateVehicleAsync(userId, id, request);

                return Ok(new { statusCode = 200, message = "Vehicle updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(string id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                int userId = int.Parse(userIdClaim);
                await _vehicleService.DeleteVehicleAsync(userId, id);

                return Ok(new { statusCode = 200, message = "Vehicle deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}
