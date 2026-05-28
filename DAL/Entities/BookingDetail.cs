using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class BookingDetail
    {
        [Key]
        public int DetailId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string LicensePlate { get; set; } = null!;

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service Service { get; set; } = null!;

        [Required]
        public decimal Price { get; set; }

        public VehicleCondition VehicleCondition { get; set; } = VehicleCondition.Clean;

        public int? ActualVehicleTypeId { get; set; }

        [ForeignKey("ActualVehicleTypeId")]
        public VehicleType? ActualVehicleType { get; set; }

        public decimal MismatchSurcharge { get; set; } = 0;
    }
}