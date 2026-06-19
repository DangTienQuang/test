using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class CarModelDTO
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public int? RequestedByUserId { get; set; }
        public int? VehicleTypeId { get; set; }
    }

    public class CreateCarModelDTO
    {
        [Required]
        public string Brand { get; set; }
        [Required]
        public string Name { get; set; }
        public int? VehicleTypeId { get; set; }
    }

    public class UpdateCarModelDTO
    {
        [Required]
        public string Brand { get; set; }
        [Required]
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class RequestCarModelDTO
    {
        [Required(ErrorMessage = "Vui lòng nhập hãng xe.")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dòng xe.")]
        public string Name { get; set; }

        public int? Year { get; set; }
        public string? Version { get; set; }

        public int? VehicleTypeId { get; set; }
    }

    public class ApproveCarModelDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn loại xe để phê duyệt.")]
        public int VehicleTypeId { get; set; }
    }
}
