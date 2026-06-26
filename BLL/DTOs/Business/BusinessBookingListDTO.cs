using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class BusinessBookingListDTO
    {
        public int BookingId { get; set; }
        public string LicensePlate { get; set; } = null!;
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } = null!;
        public decimal FinalAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
    }

    public class BusinessVehicleStatusDTO
    {
        public int? FleetWashLogId { get; set; }
        public int? BookingId { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string WashType { get; set; } = string.Empty;
        public string? LaneName { get; set; }
        public string? BranchName { get; set; }
        public DateTime? ScheduledTime { get; set; }   // from Booking (pre-checkin)
        public DateTime? CheckInTime { get; set; }      // from WashLog (post-checkin)
        public DateTime? CompletedTime { get; set; }
        public decimal WashCost { get; set; }
    }
}
