using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Business
{
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

    public class RegisterBusinessUserRequest
    {
        // --- User credentials ---
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, gồm 1 chữ hoa và 1 chữ số.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Tên công ty không được để trống.")]
        [MaxLength(100)]
        public string CompanyName { get; set; } = null!;

        public string? TaxCode { get; set; }
        public string? BusinessAddress { get; set; }
        public string? BillingEmail { get; set; }
        public string? RepresentativeName { get; set; }
        public int? PaymentTermDays { get; set; }

        [Required(ErrorMessage = "Vui lòng cung cấp giấy phép kinh doanh.")]
        public IFormFile BusinessLicense { get; set; } = null!;
        public IFormFile? AuthorizationLetter { get; set; }
    }

    public class RegisterBusinessUserResponse
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int BusinessProfileId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string ApprovalStatus { get; set; } = null!;
        public string BusinessLicenseFileUrl { get; set; } = null!;
        public string? AuthorizationLetterFileUrl { get; set; }
    }
}
