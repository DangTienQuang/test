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

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
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

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

                int userId = int.Parse(userIdClaim);
                var result = await _userService.UpdateProfileAsync(userId, request);

                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchName = null, [FromQuery] string? searchPhone = null, [FromQuery] string? searchPlate = null, [FromQuery] int? tierId = null, [FromQuery] string? status = null)
        {
            try
            {
                var result = await _userService.GetAllUsersAsync(pageIndex, pageSize, searchName, searchPhone, searchPlate, tierId, status);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(id);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ChangeUserStatus(int id, [FromBody] ChangeStatusRequest request)
        {
            try
            {
                await _userService.ChangeUserStatusAsync(id, request.Status);
                return Ok(new { statusCode = 200, message = "Success" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }

    public class ChangeStatusRequest
    {
        public string Status { get; set; }
    }
}