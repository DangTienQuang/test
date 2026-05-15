using System.ComponentModel.DataAnnotations;

namespace AutoWashPro.BLL.DTOs
{
    public class RegisterDTO
    {
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        public string FullName { get; set; }
    }

    public class LoginDTO
    {
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public string Password { get; set; }
    }

    public class AuthResponseDTO
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }
    }
}