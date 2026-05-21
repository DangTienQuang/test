using System;
using System.Security.Claims;

namespace AutoWashPro.API.Helpers
{
    public static class ClaimHelper
    {
        public static int GetUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new Exception("Unauthorized: Không tìm thấy User ID trong token.");

            if (!int.TryParse(userIdClaim, out int userId))
                throw new Exception("Unauthorized: User ID không hợp lệ.");

            return userId;
        }

        public static string GetUserRole(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        public static string GetUserPhone(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.MobilePhone)?.Value ?? string.Empty;
        }
    }
}
