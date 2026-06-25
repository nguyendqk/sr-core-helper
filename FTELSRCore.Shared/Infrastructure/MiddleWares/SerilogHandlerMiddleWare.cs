using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Serilog.Core;
using Serilog.Core.Enrichers;
using System.Security.Claims;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public class SerilogHandlerMiddleWare(RequestDelegate next, string serviceName)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            string userAgent = string.Empty;

            string getUserAgent = ConvertHelpers.GetUserAgent(context);

            if (!string.IsNullOrWhiteSpace(getUserAgent))
            {
                userAgent = getUserAgent;
            }
            else
            {
                context.Request.Headers[HeaderConstant.UserAgentHeaderKey] = serviceName ?? CommonBaseConstant.Anonymous;
            }

            //string permissionsName = CommonBaseConstant.Anonymous;

            //if (context.User.Claims?.Where(
            //        x => x.Type.Equals(ClaimTypesConstant.SRPermissions, StringComparison.CurrentCultureIgnoreCase))
            //    .Select(item => item.Value).ToList() is List<string> permissions && permissions.Count > 0)
            //{
            //    permissionsName = string.Join(DelimiterConstant.CHAR_COMMA, permissions);
            //}

            string roleName = CommonBaseConstant.Anonymous;

            RoleSR roleDataName = RoleSR.ONLY_CREATE;

            if (context.User.Claims.Where(
                    x => x.Type.Equals(ClaimTypesConstant.SRRoles, StringComparison.CurrentCultureIgnoreCase))
                .Select(item => item.Value).ToList() is List<string> { Count: > 0 } roles)
            {
                roleDataName = RoleDataConstant.GetRoleData(roles);

                roleName = string.Join(DelimiterConstant.CHAR_COMMA, roles);
            }

            string username = CommonBaseConstant.Anonymous;

            if ((context.User.FindFirst(ClaimTypes.Name))?.Value is string userName
                && !string.IsNullOrWhiteSpace(userName))
            {
                username = userName;
            }

            ILogEventEnricher[] enrichers =
            [
                new PropertyEnricher(SerilogConstant.UserPropertyName, username ?? CommonBaseConstant.Anonymous),
                new PropertyEnricher(SerilogConstant.UserAgentPropertyName, userAgent ?? CommonBaseConstant.Anonymous),
                new PropertyEnricher(SerilogConstant.UserInfoPropertyName , $"[User: {username} - Roles: {roleName} - RoleData: {roleDataName}]")
            ];

            using (LogContext.Push(enrichers))
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                await next(context);
            }
        }
    }
}