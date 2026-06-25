using System.Security.Claims;

namespace FTELSRCore.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetMemberCredentialId(this ClaimsPrincipal principal, string type)
        {
            var value = principal.FindFirst(type)?.Value;
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static string Get(this ClaimsPrincipal principal, string type)
        {
            return principal.FindFirst(type)?.Value;
        }

        public static string GetEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value;
        }

        public static string GetRole(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Role)?.Value;
        }

        public static TEnum? GetEnum<TEnum>(this ClaimsPrincipal principal, string claimType) where TEnum : struct
        {
            var value = principal.FindFirst(claimType)?.Value;
            return System.Enum.TryParse(value, out TEnum result) ? result : null;
        }
    }
}