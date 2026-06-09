using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Fleet
{
    public class CreateFleetVehicleDTO
    {
        public string LicensePlate { get; set; } = null!;

        public string VehicleType { get; set; } = null!;

        public string Brand { get; set; } = null!;

        public string Model { get; set; } = null!;

        public string? DriverName { get; set; }

        public string? EmployeeId { get; set; }
    }
}
