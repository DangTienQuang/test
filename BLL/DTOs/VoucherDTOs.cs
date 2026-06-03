using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class VoucherResponseDTO
    {
        public int VoucherId { get; set; }
        public required string Code { get; set; }
        public decimal DiscountAmount { get; set; }
        public int PointsRequired { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public DateTime? UsedDate { get; set; }
        public AutoWashPro.DAL.Enums.VoucherType VoucherType { get; set; }
        public string? ImageUrl { get; set; }
        public int? RequiredTierId { get; set; }
        public string? RequiredTierName { get; set; }
        public TimeSpan? ValidStartTime { get; set; }
        public TimeSpan? ValidEndTime { get; set; }
    }

    public class RedeemVoucherRequestDTO
    {
        [Required(ErrorMessage = "Voucher ID không được để trống.")]
        public int VoucherId { get; set; }
    }

    public class AdminVoucherDTO
    {
        public int VoucherId { get; set; }
        public required string Code { get; set; }
        public decimal DiscountAmount { get; set; }
        public int MaxUsages { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int PointsRequired { get; set; }
        public int RedeemedCount { get; set; }
        public AutoWashPro.DAL.Enums.VoucherType VoucherType { get; set; }
        public string? ImageUrl { get; set; }
        public int? RequiredTierId { get; set; }
        public string? RequiredTierName { get; set; }
        public TimeSpan? ValidStartTime { get; set; }
        public TimeSpan? ValidEndTime { get; set; }
    }

    public class CreateOrUpdateVoucherDTO
    {
        [Required(ErrorMessage = "Mã voucher không được để trống.")]
        [MaxLength(50)]
        [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Mã voucher không được chỉ chứa khoảng trắng.")]
        public required string Code { get; set; }

        [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "Số tiền giảm không hợp lệ.")]
        public decimal DiscountAmount { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxUsages { get; set; } = 0;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Range(0, int.MaxValue)]
        public int PointsRequired { get; set; }

        public AutoWashPro.DAL.Enums.VoucherType VoucherType { get; set; } = AutoWashPro.DAL.Enums.VoucherType.Discount;
        public string? ImageUrl { get; set; }
        public int? RequiredTierId { get; set; }
        public TimeSpan? ValidStartTime { get; set; }
        public TimeSpan? ValidEndTime { get; set; }
    }
}
