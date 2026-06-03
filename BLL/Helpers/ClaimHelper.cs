using System;
using System.Security.Claims;

namespace BLL.Helpers
{
    public static class ClaimHelper
    {
        public static int GetUserId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new UnauthorizedAccessException("Không tìm thấy UserId trong Token. Vui lòng đăng nhập lại.");

            if (!int.TryParse(claim.Value, out int userId))
                throw new UnauthorizedAccessException("Định dạng UserId trong Token không hợp lệ.");

            return userId;
        }

        public static string GetRole(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }
    }
}