using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Fleet
{
    public class FleetVehicleImportDTO
    {
        public string LicensePlate { get; set; } = null!;

        public int VehicleTypeId { get; set; }

        public string Brand { get; set; } = null!;

        public string Model { get; set; } = null!;

        public string? DriverName { get; set; }

        public string? EmployeeCode { get; set; }
    }
}
