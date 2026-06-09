using AutoWashPro.DAL.Entities;
using System.ComponentModel.DataAnnotations;

namespace DAL.Entities
{
    public class FleetVehicle
    {
        [Key]
        public int FleetVehicleId { get; set; }

        public int BusinessProfileId { get; set; }
        [MaxLength(20)]
        public string LicensePlate { get; set; } = null!;

        public int VehicleTypeId { get; set; }
        [MaxLength(50)]
        public string Brand { get; set; } = null!;
        [MaxLength(50)]
        public string Model { get; set; } = null!;

        public string? DriverName { get; set; }

        public string? EmployeeCode { get; set; }

        public string Status { get; set; } = "PendingApproval";

        public DateTime CreatedAt { get; set; }
        public int FleetImportBatchId { get; set; }
        public string? RejectionReason { get; set; }

        public FleetImportBatch FleetImportBatch { get; set; } = null!;

        public BusinessProfile BusinessProfile { get; set; } = null!;

        public VehicleType VehicleType { get; set; } = null!;
    }
}