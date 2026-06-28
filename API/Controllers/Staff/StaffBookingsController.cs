using AutoWashPro.BLL.DTOs;

using AutoWashPro.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers.Staff
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

        [HttpPut("status-by-license-plate")]
        public async Task<IActionResult> UpdateBookingStatusByLicensePlate([FromBody] UpdateBookingStatusByPlateDTO request)
        {
            var result = await _bookingService.UpdateBookingStatusByLicensePlateAsync(request.LicensePlate, request.NewStatus);
            return Ok(new { statusCode = 200, message = $"Đã cập nhật trạng thái xe {request.LicensePlate} thành: {request.NewStatus}", data = result });
        }
        [HttpGet("by-license-plate/{licensePlate}")]
        public async Task<IActionResult> GetBookingsByLicensePlate(string licensePlate)
        {
            var result = await _bookingService.GetBookingsByLicensePlateAsync(licensePlate);
            return Ok(new { statusCode = 200, message = "Success", data = result });
        }
        [HttpPut("{id}/no-show")]
        public async Task<IActionResult> MarkAsNoShow(int id)
        {
            await _bookingService.MarkAsNoShowAsync(id);
            return Ok(new { statusCode = 200, message = "Đã đánh dấu khách No-Show thành công." });
        }

        [HttpPut("{detailId}/report-mismatch")]
        public async Task<IActionResult> ReportMismatch(int detailId, [FromQuery] AutoWashPro.BLL.Enums.VehicleConditionEnum condition, [FromQuery] int actualTypeId)
        {
            await _bookingService.ReportMismatchAsync(detailId, condition, actualTypeId);
            return Ok(new { statusCode = 200, message = "Đã cập nhật tình trạng xe và tính lại phụ phí thành công." });
        }
        [HttpPost("force-cancel")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ForceCancelBookings([FromBody] ForceCancelRequestDTO request)
        {
            await _bookingService.ForceCancelBookingsAsync(request);
            return Ok(new { statusCode = 200, message = "Đã hủy các lịch hẹn thành công, hoàn tiền và gửi email thông báo tới khách hàng." });
        }
    }
}
