using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Created("", new { statusCode = 201, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { statusCode = 401, message = ex.Message });
            }
        }
    }
}