using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
    public class CreateBusinessBookingDTO
    {
        public int FleetVehicleId { get; set; }
        public int BranchId { get; set; }
        public int SlotId { get; set; }
        public DateTime ScheduledTime { get; set; }
        public List<int> ServiceIds { get; set; } = [];
    }
}
