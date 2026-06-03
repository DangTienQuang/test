using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        public decimal DiscountAmount { get; set; }
        public int MaxUsages { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int PointsRequired { get; set; }

        public AutoWashPro.DAL.Enums.VoucherType VoucherType { get; set; } = AutoWashPro.DAL.Enums.VoucherType.Discount;

        public string? ImageUrl { get; set; }

        public int? RequiredTierId { get; set; }

        [ForeignKey("RequiredTierId")]
        public Tier? RequiredTier { get; set; }

        public TimeSpan? ValidStartTime { get; set; }
        public TimeSpan? ValidEndTime { get; set; }
    }
}