using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class ServiceDTO
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public List<ServicePriceDTO> Prices { get; set; }
    }

    public class ServicePriceDTO
    {
        public int VehicleTypeId { get; set; }
        public string VehicleTypeName { get; set; }
        public int BranchId { get; set; }
        public decimal Price { get; set; }
        public int EstimatedDurationMinutes { get; set; }
    }
    public class CreateOrUpdateServiceDTO
    {
        [Required(ErrorMessage = "Tên dịch vụ không được để trống.")]
        public string ServiceName { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Phải cấu hình ít nhất 1 mức giá cho dịch vụ.")]
        [MinLength(1, ErrorMessage = "Phải cấu hình ít nhất 1 mức giá cho dịch vụ.")]
        public List<CreateServicePriceDTO> Prices { get; set; }
    }

    public class CreateServicePriceDTO
    {
        [Required]
        public int VehicleTypeId { get; set; }

        [Required]
        public int BranchId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền không hợp lệ.")]
        public decimal Price { get; set; }

        [Required]
        [Range(5, 600, ErrorMessage = "Thời gian thực hiện (phút) phải từ 5 đến 600.")]
        public int EstimatedDurationMinutes { get; set; }
    }
}