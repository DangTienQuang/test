using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class VehicleCheckInDTO
    {
        public int BookingDetailId { get; set; }
    }

    public class VehicleCompleteDTO
    {
        public int BookingDetailId { get; set; }

        public decimal ActualPrice { get; set; }
    }

    public class VehicleNoShowDTO
    {
        public int BookingDetailId { get; set; }
    }
}
