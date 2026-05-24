using AutoWashPro.BLL.DTOs;
using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/bookings")]
    [ApiController]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new AutoWashPro.BLL.Exceptions.UnauthorizedException("Không tìm thấy thông tin xác thực (UserId). Vui lòng đăng nhập lại.");
            }
            return userId;
        }

        [HttpGet("slots")]
        public async Task<IActionResult> GetAvailableSlots([FromQuery] DateTime targetDate)
        {
            int userId = GetUserId();
            var result = await _bookingService.GetAvailableSlotsAsync(userId, targetDate);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDTO request)
        {
            int userId = GetUserId();
            var result = await _bookingService.CreateBookingAsync(userId, request);
            return Created("", new { statusCode = 201, message = "Đặt lịch và thanh toán cọc thành công.", data = result });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyBookings()
        {
            int userId = GetUserId();
            var result = await _bookingService.GetMyBookingsAsync(userId);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingById(int id)
        {
            int userId = GetUserId();
            var result = await _bookingService.GetBookingByIdAsync(userId, id);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            int userId = GetUserId();
            await _bookingService.CancelBookingAsync(userId, id);
            return Ok(new { statusCode = 200, message = "Đã hủy lịch thành công." });
        }
    }
}