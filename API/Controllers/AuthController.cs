using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
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
            var result = await _authService.RegisterAsync(request);
            return Created("", new { statusCode = 201, message = "Success", data = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            return Ok(new { statusCode = 200, message = "Xác thực email thành công.", data = result });
        }
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDTO request)
        {
            var response = await _authService.ResendOtpAsync(request);
            return Ok(new { statusCode = 200, message = "Gửi lại mã OTP thành công", data = response });
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [Authorize] 
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized(new { statusCode = 401, message = "Unauthorized" });

            int userId = int.Parse(userIdClaim);
            await _authService.ChangePasswordAsync(userId, request);

            return Ok(new { statusCode = 200, message = "Đổi mật khẩu thành công." });
        }
    }
}
