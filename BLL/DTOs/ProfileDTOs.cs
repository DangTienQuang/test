using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class UserProfileDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string TierName { get; set; }
        public double ChurnScore { get; set; }
        public System.Collections.Generic.List<VehicleDTO> Vehicles { get; set; }
    }

    public class VehicleDTO
    {
        public string LicensePlate { get; set; }
        public string VehicleType { get; set; }
    }

    public class CreateVehicleDTO
    {
        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        [RegularExpression(@"^[0-9]{2}[A-Z0-9]-[0-9]{3,5}(\.[0-9]{2})?$", ErrorMessage = "Biển số xe không hợp lệ (VD: 51H-123.45).")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "Loại xe không được để trống.")]
        public string VehicleType { get; set; }
    }
}