using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class CustomerProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }

        [ForeignKey("Tier")]
        public int TierId { get; set; }
        public Tier Tier { get; set; } = null!;

        public double ChurnScore { get; set; }
        public DateTime? LastVisitDate { get; set; }

        [MaxLength(20)]
        public string? ReferralCode { get; set; }

        public int? ReferredById { get; set; }

        public int TotalPoint { get; set; } = 0;

        public int PromotionPoint { get; set; } = 0;

        public int TrustScore { get; set; } = 100;

        [Timestamp]
        public DateTime? RowVersion { get; set; }
    }
}