using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class BusinessBookingResponseDTO
    {
        public int BookingId { get; set; }

        public string LicensePlate { get; set; } = null!;

        public decimal OriginalPrice { get; set; }

        public decimal FinalAmount { get; set; }

        public string Status { get; set; } = null!;
    }

    public class BusinessBookingDetailDTO
    {
        public int BookingId { get; set; }
        public string LicensePlate { get; set; } = null!;
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } = null!;
        public decimal OriginalPrice { get; set; }
        public decimal FinalAmount { get; set; }
        public List<string> Services { get; set; } = [];
    }
}
