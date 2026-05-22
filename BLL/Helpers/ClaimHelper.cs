using System.Security.Claims;

namespace BLL.Helpers
{
    public static class ClaimHelper
    {
        public static int GetUserId(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new Exception("UserId claim not found.");

            return int.Parse(claim.Value);
        }

        public static string GetRole(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value
                   ?? string.Empty;
        }
    }
}