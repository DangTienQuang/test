using System;

namespace AutoWashPro.BLL.DTOs
{
    public class StaffBookingDetailDTO
    {
        public int DetailId { get; set; }
        public int BookingId { get; set; }
        public string LicensePlate { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public string VehicleTypeName { get; set; } = null!;
        public string Status { get; set; } = null!; // CheckedIn, Processing, Completed
    }
}
