using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class ServiceResponseDTO
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal BasePrice { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class CreateBookingDTO
    {
        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        public string LicensePlate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dịch vụ.")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian đặt lịch.")]
        public DateTime ScheduledTime { get; set; }
    }

    public class BookingResponseDTO
    {
        public int BookingId { get; set; }
        public string LicensePlate { get; set; }
        public string ServiceName { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; }
    }

    public class CreateServiceDTO
    {
        [Required(ErrorMessage = "Tên dịch vụ không được để trống.")]
        public string ServiceName { get; set; }

        [Range(10000, 10000000, ErrorMessage = "Giá tiền tối thiểu là 10.000 VNĐ.")]
        public decimal BasePrice { get; set; }

        [Range(5, 300, ErrorMessage = "Thời gian thực hiện phải từ 5 đến 300 phút.")]
        public int DurationMinutes { get; set; }
    }
}
