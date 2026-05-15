using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class CreateTierDTO
    {
        [Required(ErrorMessage = "Tên hạng không được để trống.")]
        public string TierName { get; set; }

        [Range(1.0, 5.0, ErrorMessage = "Hệ số nhân điểm phải nằm trong khoảng 1.0 đến 5.0.")]
        public double PointMultiplier { get; set; }

        [Range(1, 365, ErrorMessage = "Số ngày đặt trước phải từ 1 đến 365 ngày.")]
        public int BookingWindowDays { get; set; }
    }

    public class TierResponseDTO
    {
        public int TierId { get; set; }
        public string TierName { get; set; }
        public double PointMultiplier { get; set; }
        public int BookingWindowDays { get; set; }
    }
}
