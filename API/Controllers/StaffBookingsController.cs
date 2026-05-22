using AutoWashPro.BLL.Services;    
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoWashPro.API.Controllers
{
    [Route("api/v1/admin/bookings")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class StaffBookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public StaffBookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBookingsByDate([FromQuery] DateTime targetDate)
        {
            try
            {
                var result = await _bookingService.GetAllBookingsByDateAsync(targetDate);
                return Ok(new { statusCode = 200, message = "Success", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromQuery] string newStatus)
        {
            try
            {
                await _bookingService.UpdateBookingStatusAsync(id, newStatus);
                return Ok(new { statusCode = 200, message = $"Đã cập nhật trạng thái thành: {newStatus}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { statusCode = 400, message = ex.Message });
            }
        }
    }
}