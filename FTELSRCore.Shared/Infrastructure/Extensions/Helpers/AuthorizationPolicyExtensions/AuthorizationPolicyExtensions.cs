using Microsoft.AspNetCore.Authorization;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.AuthorizationPolicyExtensions
{
    public static class AuthorizationPolicyExtensions
    {
        public static Action<AuthorizationPolicyBuilder> AddAuthorizationPolicy(string authorizationPolicy)
        {
            return (builder) =>
            {
                builder.RequireAuthenticatedUser()
                .RequireAssertion(
                    context => context.User.HasClaim(
                        c => c.Type.Equals(ClaimTypesConstant.Permissions)
                            && c.Value.Any(value => value.Equals(authorizationPolicy))));
            };
        }
    }
}