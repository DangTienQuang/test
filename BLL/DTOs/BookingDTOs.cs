using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class TimeSlotResponseDTO
    {
        public int SlotId { get; set; }
        public required string TimeRange { get; set; } 
        public bool IsAvailable { get; set; }
        public string? Reason { get; set; }
    }

    public class CreateBookingDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn xe.")]
        [MinLength(1, ErrorMessage = "Biển số xe không được để trống.")]
        [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Biển số xe không hợp lệ (không được chỉ chứa khoảng trắng).")]
        public required string LicensePlate { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        public int SlotId { get; set; }

        public int PointsToUse { get; set; } = 0;

        public int? VoucherId { get; set; }
    }

    public class BookingResponseDTO
    {
        public int BookingId { get; set; }
        public required string LicensePlate { get; set; }
        public required string ServiceName { get; set; }
        public DateTime ScheduledTime { get; set; }
        public required string Status { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal PointDiscountAmount { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }
}
