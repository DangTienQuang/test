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
            var result = await _bookingService.GetAllBookingsByDateAsync(targetDate);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromQuery] string newStatus)
        {
            await _bookingService.UpdateBookingStatusAsync(id, newStatus);
            return Ok(new { statusCode = 200, message = $"Đã cập nhật trạng thái thành: {newStatus}" });
        }

        [HttpPut("{id}/no-show")]
        public async Task<IActionResult> MarkAsNoShow(int id)
        {
            await _bookingService.MarkAsNoShowAsync(id);
            return Ok(new { statusCode = 200, message = "Đã đánh dấu khách No-Show thành công." });
        }

        [HttpPut("{detailId}/report-mismatch")]
        public async Task<IActionResult> ReportMismatch(int detailId, [FromQuery] AutoWashPro.DAL.Entities.VehicleCondition condition, [FromQuery] int actualTypeId)
        {
            await _bookingService.ReportMismatchAsync(detailId, condition, actualTypeId);
            return Ok(new { statusCode = 200, message = "Đã cập nhật tình trạng xe và tính lại phụ phí thành công." });
        }
    }
}