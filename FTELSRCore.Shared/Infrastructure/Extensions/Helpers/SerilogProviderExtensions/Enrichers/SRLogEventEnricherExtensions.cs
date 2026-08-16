using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Principal;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.SerilogProviderExtensions.Enrichers
{
    public class SRLogEventEnricherExtensions(IHttpContextAccessor contextAccessors, string serviceName) : ILogEventEnricher
    {
        public SRLogEventEnricherExtensions()
            : this(new HttpContextAccessor(), CommonBaseConstant.System)
        {
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            HttpContext context = contextAccessors.HttpContext;

            AddPropertyIfAbsentInSerilog(logEvent: logEvent,
                                         propertyFactory: propertyFactory,
                                         serilogName: SerilogConstant.ServiceNamePropertyName,
                                         data: serviceName);

            if (context is null)
            {
                AddPropertyIfAbsentInSerilog(logEvent: logEvent,
                                             propertyFactory: propertyFactory,
                                             serilogName: SerilogConstant.UserInfoPropertyName,
                                             data: CommonBaseConstant.Anonymous);
                return;
            }

            string ipAddress = string.Empty;

            string getIPAddress = ConvertHelpers.GetClientIpAddress(context);

            if (!string.IsNullOrWhiteSpace(getIPAddress))
            {
                ipAddress = getIPAddress;
            }
            else
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            AddPropertyIfAbsentInSerilog(logEvent: logEvent,
                                         propertyFactory: propertyFactory,
                                         serilogName: SerilogConstant.ClientIpPropertyName,
                                         data: ipAddress);

            string correlationId;

            if (context.Request.Headers.TryGetValue(
                    key: HeaderConstant.CorrelationIdHeaderKey, out StringValues correlationIds))
            {
                correlationId = correlationIds.FirstOrDefault();
            }
            else
            {
                correlationId = Activity.Current?.TraceId.ToString();

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    correlationId = Guid.NewGuid().ToString("N");
                }
            }

            AddPropertyIfAbsentInSerilog(logEvent: logEvent,
                                         propertyFactory: propertyFactory,
                                         serilogName: SerilogConstant.CorrelationIdPropertyName,
                                         data: correlationId);

            IIdentity identity = context.User?.Identity;

            if (identity is null || !identity.IsAuthenticated)
            {
                AddPropertyIfAbsentInSerilog(logEvent: logEvent,
                                             propertyFactory: propertyFactory,
                                             serilogName: SerilogConstant.UserInfoPropertyName,
                                             data: CommonBaseConstant.Anonymous);

                return;
            }

            //string permissionsName = CommonBaseConstant.Anonymous;

            //if (context.User.Claims?.Where(
            //        x => x.Type.Equals(ClaimTypesConstant.SRPermissions, StringComparison.CurrentCultureIgnoreCase))
            //    .Select(item => item.Value).ToList() is List<string> permissions && permissions.Count > 0)
            //{
            //    permissionsName = string.Join(DelimiterConstant.CHAR_COMMA, permissions);
            //}

            RoleSR roleDataName = RoleSR.ONLY_CREATE;
            string roleName = CommonBaseConstant.Anonymous;

            if (context.User.Claims?.Where(
                    x => x.Type.Equals(ClaimTypesConstant.SRRoles, StringComparison.CurrentCultureIgnoreCase))
                .Select(item => item.Value).ToList() is List<string> roles && roles.Count > 0)
            {
                roleDataName = RoleDataConstant.GetRoleData(roles);
                roleName = string.Join(DelimiterConstant.CHAR_COMMA, roles);
            }

            string username = CommonBaseConstant.Anonymous;

            if (context.User?.FindFirst(ClaimTypes.Name)?.Value is string userName
                && !string.IsNullOrWhiteSpace(userName))
            {
                username = userName;
            }

            string userInfo = $"[User: {username} - Roles: {roleName} - RoleData: {roleDataName} - IP: {ipAddress}]";

            AddPropertyIfAbsentInSerilog(logEvent: logEvent,
                                         propertyFactory: propertyFactory,
                                         serilogName: SerilogConstant.UserInfoPropertyName,
                                         data: userInfo);
        }

        private static void AddPropertyIfAbsentInSerilog(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, string serilogName, string data)
        {
            LogEventProperty userProperty =
               propertyFactory.CreateProperty(serilogName, data);

            logEvent.AddPropertyIfAbsent(userProperty);
        }
    }
}