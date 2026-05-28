using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }

        [MaxLength(255)]
        public string? FallbackQrCode { get; set; }

        public int TrustScorePenalty { get; set; } = 0;

        [Required]
        public DateTime ScheduledTime { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public ICollection<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();

        [Required]
        public decimal OriginalPrice { get; set; }

        public int PointsUsed { get; set; } = 0;

        public decimal PointDiscountAmount { get; set; } = 0;

        public int? AppliedVoucherId { get; set; }

        public decimal VoucherDiscountAmount { get; set; } = 0;

        [Required]
        public decimal FinalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}