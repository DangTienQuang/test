using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BLL.DTOs.Business
{
    public class CreateBusinessProfileRequest
    {
        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }

        public string? BusinessAddress { get; set; }

        public string? BillingEmail { get; set; }

        public string? RepresentativeName { get; set; }

        public int PaymentTermDays { get; set; }

        public IFormFile BusinessLicense { get; set; } = null!;

        public IFormFile? AuthorizationLetter { get; set; }
    }

    public class CreateBusinessProfileDTO
    {
        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }

        public string? BusinessAddress { get; set; }

        public string? BillingEmail { get; set; }

        public string? RepresentativeName { get; set; }

        public int PaymentTermDays { get; set; }

        public string BusinessLicenseFileUrl { get; set; } = null!;

        public string? AuthorizationLetterFileUrl { get; set; }
    }

    public class BusinessProfileResponseDTO
    {
        public int BusinessProfileId { get; set; }

        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }

        public string? BusinessAddress { get; set; }

        public string ApprovalStatus { get; set; } = null!;

        public string? RejectionReason { get; set; }

        public string BusinessLicenseFileUrl { get; set; } = null!;

        public string? AuthorizationLetterFileUrl { get; set; }
    }
}
