using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoWashPro.DAL.Entities
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int WalletId { get; set; }
        [ForeignKey("WalletId")]
        public Wallet Wallet { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public required string TransactionType { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Description { get; set; }

        public int? ReferenceBookingId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}