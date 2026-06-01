using System;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class TimeSlotAdminResponseDTO
    {
        public int SlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int MaxCapacity { get; set; }
        public bool IsVipOnly { get; set; }
    }

    public class CreateTimeSlotDTO
    {
        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sức chứa (MaxCapacity) phải lớn hơn 0.")]
        public int MaxCapacity { get; set; } = 3;

        public bool IsVipOnly { get; set; } = false;
    }

    public class UpdateTimeSlotDTO
    {
        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sức chứa (MaxCapacity) phải lớn hơn 0.")]
        public int MaxCapacity { get; set; }

        public bool IsVipOnly { get; set; }
    }

    public class SlotAvailabilityDTO
    {
        public int SlotId { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
