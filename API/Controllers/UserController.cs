using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                int userId = int.Parse(userIdClaim);
                var result = await _userService.GetProfileAsync(userId);

                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPost("vehicles")]
        public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                int userId = int.Parse(userIdClaim);
                await _userService.AddVehicleAsync(userId, request);

                return Created("", new { statusCode = 201, message = "Success" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}