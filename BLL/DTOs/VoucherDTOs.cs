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
    }

    public class RedeemVoucherRequestDTO
    {
        [Required(ErrorMessage = "Voucher ID không được để trống.")]
        public int VoucherId { get; set; }
    }
}
