using AutoWashPro.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
    public class BusinessProfile
    {
        public int BusinessProfileId { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }

        public string? BusinessAddress { get; set; }

        public string? BillingEmail { get; set; }

        public string? RepresentativeName { get; set; }

        public int? PaymentTermDays { get; set; }
        public string BillingCycle { get; set; } = "Monthly"; //Monthly, Quarterly, Yearly

        public DateTime CreatedAt { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";

        public string? RejectionReason { get; set; }
        public int? ReviewedByUserId { get; set; }
        [ForeignKey(nameof(ReviewedByUserId))]
        public User? ReviewedByUser { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string BusinessLicenseFileUrl { get; set; } = null!;

        public string? AuthorizationLetterFileUrl { get; set; }
        public decimal MonthlyCreditLimit { get; set; }

        public decimal CurrentMonthUsage { get; set; }

        public decimal DiscountPercent { get; set; }

        public DateTime ContractStartDate { get; set; }

        public DateTime ContractEndDate { get; set; }

        public bool IsContractActive { get; set; }

        public User User { get; set; } = null!;
    }

}
