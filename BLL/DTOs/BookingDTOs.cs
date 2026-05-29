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

    public class VehicleBookingItemDTO
    {
        [Required]
        [MaxLength(20)]
        public required string LicensePlate { get; set; }

        [Required]
        public int ServiceId { get; set; }
    }

    public class CreateBookingDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 xe.")]
        public required List<VehicleBookingItemDTO> Vehicles { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        public int SlotId { get; set; }

        public int PointsToUse { get; set; } = 0;

        public int? VoucherId { get; set; }
    }

    public class CreateWalkInBookingDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 xe.")]
        public required List<VehicleBookingItemDTO> Vehicles { get; set; }

        public int UserId { get; set; } // Walk-ins might have user ID provided by Staff

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

    public class UpdateVehicleConditionDTO
    {
        [Required]
        public int DetailId { get; set; }

        [Required]
        public DAL.Entities.VehicleCondition Condition { get; set; }

        public int? ActualVehicleTypeId { get; set; }
    }
}

namespace AutoWashPro.BLL.DTOs
{
    public class ForceCancelRequestDTO
    {
        public int? TimeSlotId { get; set; }

        public DateTime? AffectedDate { get; set; }

        [Required(ErrorMessage = "Reason is required to notify customers.")]
        public required string Reason { get; set; }
    }
}
