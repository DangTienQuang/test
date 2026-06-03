using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.API.Models.Requests
{
    public class CreateVehicleRequest
    {
        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        [RegularExpression(@"^[0-9]{2}[A-Z0-9]-[0-9]{3,5}(\.[0-9]{2})?$", ErrorMessage = "Biển số xe không hợp lệ (VD: 51H-123.45).")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại xe.")]
        public int VehicleTypeId { get; set; }

        public string? RegistrationPhotoUrl { get; set; }
        public IFormFile? PhotoFile { get; set; }
        public string? UserNote { get; set; }
        public int? CarModelId { get; set; }
        public string? CarModel { get; set; }
    }

    public class UpdateVehicleRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn loại xe.")]
        public int VehicleTypeId { get; set; }
        public IFormFile? PhotoFile { get; set; }
        public string? UserNote { get; set; }
        public int? CarModelId { get; set; }
        public string? CarModel { get; set; }
    }
}
