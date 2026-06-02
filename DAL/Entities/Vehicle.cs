using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Vehicle
    {
        [Key]
        [MaxLength(20)]
        public string LicensePlate { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User User { get; set; }

        [Required]
        public int VehicleTypeId { get; set; }

        [ForeignKey("VehicleTypeId")]
        public VehicleType VehicleType { get; set; }

        public string? RegistrationPhotoUrl { get; set; }

        public string? UserNote { get; set; }

        public int? CarModelId { get; set; }

        [ForeignKey("CarModelId")]
        public CarModel CarModelEntity { get; set; }

        public string? CarModel { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}