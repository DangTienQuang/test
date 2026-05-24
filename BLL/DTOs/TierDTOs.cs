using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class TierResponseDTO
    {
        public int TierId { get; set; }
        public string TierName { get; set; }
        public double PointMultiplier { get; set; }
        public int BookingWindowDays { get; set; }
        public int MinAccumulatedPoints { get; set; }
    }

    public class CreateTierDTO
    {
        [Required(ErrorMessage = "Tên hạng không được để trống.")]
        [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Tên hạng không được chỉ chứa khoảng trắng.")]
        public string TierName { get; set; }

        [Range(1.0, 5.0, ErrorMessage = "Hệ số nhân điểm phải từ 1.0 đến 5.0.")]
        public double PointMultiplier { get; set; }

        [Range(1, 30, ErrorMessage = "Số ngày đặt trước phải từ 1 đến 30 ngày.")]
        public int BookingWindowDays { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Điểm tối thiểu không hợp lệ.")]
        public int MinAccumulatedPoints { get; set; }
    }

    public class UpdateTierDTO : CreateTierDTO { }
}