using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class UserProfileDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string TierName { get; set; }
        public int TotalPoint { get; set; }
        public int PromotionPoint { get; set; }
        public double ChurnScore { get; set; }
        public List<VehicleDTO> Vehicles { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Email { get; internal set; }
        public string Status { get; internal set; }
    }

    public class VehicleDTO
    {
        public string LicensePlate { get; set; }
        public int VehicleTypeId { get; set; }
        public string VehicleType { get; set; }
        public string? RegistrationPhotoUrl { get; set; }
        public string? CarModel { get; set; }
    }

    public class CreateVehicleDTO
    {
        [Required(ErrorMessage = "Biển số xe không được để trống.")]
        [RegularExpression(@"^[0-9]{2}[A-Z0-9]-[0-9]{3,5}(\.[0-9]{2})?$", ErrorMessage = "Biển số xe không hợp lệ (VD: 51H-123.45).")]
        public string LicensePlate { get; set; }

        public int? VehicleTypeId { get; set; }

        public string? RegistrationPhotoUrl { get; set; }
        public IFormFile? PhotoFile { get; set; }
        public string? UserNote { get; set; }
        public int? CarModelId { get; set; }
        public string? CarModel { get; set; }
    }

    public class UpdateVehicleDTO
    {
        public int? VehicleTypeId { get; set; }
        public IFormFile? PhotoFile { get; set; }
        public string? UserNote { get; set; }
        public int? CarModelId { get; set; }
        public string? CarModel { get; set; }
    }

    public class UpdateUserProfileDTO
    {
        [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Họ tên không được chỉ chứa khoảng trắng.")]
        public string? FullName { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }

    public class UpdateUserStatusDTO
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [RegularExpression("^(Active|Blocked)$", ErrorMessage = "Trạng thái chỉ được phép là 'Active' hoặc 'Blocked'.")]
        public string Status { get; set; }
    }

    public class PagedResultDTO<T>
    {
        public List<T> Items { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    public class UserAdminSummaryDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string TierName { get; set; }
        public string Status { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public string? Email { get; internal set; }
    }

    public class VehicleRecognitionDTO
    {
        public string LicensePlate { get; set; }
        public string VehicleType { get; set; }
        public string OwnerName { get; set; }
        public string OwnerPhone { get; set; }
        public string TierName { get; set; }
        public bool HasActiveBooking { get; set; }
        public int? ActiveBookingId { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? CarModel { get; set; }
    }

    public class AdminOtherVehicleDTO
    {
        public string LicensePlate { get; set; }
        public int VehicleTypeId { get; set; }
        public string VehicleTypeName { get; set; }
        public int? UserId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerPhone { get; set; }
        public string? RegistrationPhotoUrl { get; set; }
        public string? UserNote { get; set; }
        public string? CarModel { get; set; }
    }

    public class UpdateVehicleTypeAdminDTO
    {
        [Required(ErrorMessage = "Vui lòng chọn loại xe.")]
        public int VehicleTypeId { get; set; }
    }

    public class ApproveVehicleTypeRequestDTO
    {
        [StringLength(50, ErrorMessage = "Tên loại xe tối đa 50 ký tự.")]
        public string? CustomizedTypeName { get; set; }

        public string? Description { get; set; }
    }
}