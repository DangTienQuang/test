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

        [Required]
        [MaxLength(20)]
        public required string LicensePlate { get; set; }

        [Required]
        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service Service { get; set; } = null!;

        [Required]
        public DateTime ScheduledTime { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

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