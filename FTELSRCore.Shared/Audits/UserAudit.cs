using FTELSRCore.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Security.Claims;

namespace FTELSRCore.Audits
{
    public record UserAudit : IUserAudit
    {
        private readonly Lazy<IHttpContextAccessor> _contextAccessor;

        public UserAudit(Lazy<IHttpContextAccessor> httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;

            ClaimsPrincipal claimsPrincipal =
                httpContextAccessor?.Value?.HttpContext?.User;

            EmployeeCode =
                ConvertHelpers.ConvertClaimsPrincipalToData(
                    claimsPrincipal: claimsPrincipal,
                    claimType: ClaimTypes.NameIdentifier,
                    setDataDefault: CommonBaseConstant.AnonymousCode);

            EmployeeUserName =
                ConvertHelpers.ConvertClaimsPrincipalToData(
                    claimsPrincipal: claimsPrincipal,
                    claimType: ClaimTypes.Name,
                    setDataDefault: CommonBaseConstant.Anonymous);

            EmployeeEmail =
                ConvertHelpers.ConvertClaimsPrincipalToData(
                    claimsPrincipal: claimsPrincipal,
                    claimType: ClaimTypes.Email,
                    setDataDefault: string.Empty);

            TitleCode =
                ConvertHelpers.ConvertClaimsPrincipalToData(
                    setDataDefault: string.Empty,
                    claimsPrincipal: claimsPrincipal,
                    claimType: nameof(TitleCode)) is string titleCodeAsString
                        && !string.IsNullOrWhiteSpace(titleCodeAsString) ? titleCodeAsString : null;

            Organization =
                ConvertHelpers.ConvertClaimsPrincipalToData(
                    claimsPrincipal: claimsPrincipal,
                    setDataDefault: CommonBaseConstant.OrganizationForISC,
                    claimType: nameof(Organization)) is string OrganizationAsString
                        && !string.IsNullOrWhiteSpace(OrganizationAsString) ? OrganizationAsString : null;

            RegionId =
              ConvertHelpers.ConvertClaimsPrincipalToData(
                  setDataDefault: default,
                  claimsPrincipal: claimsPrincipal,
                  claimType: nameof(RegionId)) is string regionIdAsString
                      && !string.IsNullOrWhiteSpace(regionIdAsString) && int.TryParse(regionIdAsString, out int regionAsInt) ? regionAsInt : null;

            BranchId =
                ConvertHelpers.ConvertClaimsPrincipalToData(
                    setDataDefault: default,
                    claimsPrincipal: claimsPrincipal,
                    claimType: nameof(BranchId)) is string branchAsString
                        && !string.IsNullOrWhiteSpace(branchAsString) && int.TryParse(branchAsString, out int branchAsInt) ? branchAsInt : null;

            LocationId =
                ConvertHelpers.ConvertClaimsPrincipalToData(
                    setDataDefault: default,
                    claimsPrincipal: claimsPrincipal,
                    claimType: nameof(LocationId)) is string locationAsString
                        && !string.IsNullOrWhiteSpace(locationAsString) && int.TryParse(locationAsString, out int locationAsInt) ? locationAsInt : null;

            ConcurrentAreas =
                claimsPrincipal?.Claims?.Where(
                    x => x.Type.Equals(ClaimTypesConstant.ConcurrentArea, StringComparison.CurrentCultureIgnoreCase))
                .SelectMany(item =>
                {
                    if (!string.IsNullOrWhiteSpace(item?.Value)
                        && item.Value.JSonTryParse(out List<GetAllConcurrentAreaEmployeeModel> data))
                    {
                        return data;
                    }

                    return [];
                })?.Where(x => x is not null)?.ToList() ?? [];

            RolesSR =
                claimsPrincipal?.Claims
                ?.Where(x => x.Type.Equals(ClaimTypesConstant.SRRoles, StringComparison.CurrentCultureIgnoreCase))
                .Select(item => item.Value).ToList() ?? [];

            RolesFTel =
                claimsPrincipal?.Claims
                ?.Where(x => x.Type.Equals(ClaimTypesConstant.FTelRoles, StringComparison.CurrentCultureIgnoreCase))
                .Select(item => item.Value).ToList() ?? [];

            Permissions =
                claimsPrincipal?.Claims
                ?.Where(x => x.Type.Equals(ClaimTypesConstant.Permissions, StringComparison.CurrentCultureIgnoreCase))
                .Select(item => item.Value).ToList() ?? [];
        }

        public int? RegionId { get; }

        public int? BranchId { get; }

        public int? LocationId { get; }

        public string TitleCode { get; }

        public string EmployeeEmail { get; }

        public string EmployeeCode { get; }

        public string EmployeeUserName { get; }

        public List<string> RolesFTel { get; }

        public List<string> RolesSR { get; }

        public List<string> Permissions { get; }

        public string Organization { get; }

        public List<GetAllConcurrentAreaEmployeeModel> ConcurrentAreas { get; }

        public RoleSR RoleData
        {
            get
            {
                return RoleDataConstant.GetRoleData(RolesSR);
            }
        }

        /// <summary>
        /// Lấy thông tin Audit theo token.
        /// </summary>
        /// <param name="defaultName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        public async Task<AuditModel> GetAuditCurrentAsync(
            string defaultName = "", CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AuditModel audit = GetAuditCurrentUser(cancellationToken);

            return await Task.FromResult(SetAudit(audit: audit,
                                                  employee: null,
                                                  defaultName: defaultName,
                                                  cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Lấy thông tin cơ bản từ token cơ bản.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        protected AuditModel GetAuditCurrentUser(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HttpContext httpContext = _contextAccessor.Value?.HttpContext;

            return new AuditModel
            {
                CreatorInfo = new CreatorInfo
                {
                    BranchId = BranchId,
                    LocationId = LocationId,

                    Role = RoleData,
                    RolesSR = RolesSR,
                    RolesFTel = RolesFTel,
                    ConcurrentAreas = ConcurrentAreas,
                    TitleCode = TitleCode ?? string.Empty,
                    Email = EmployeeEmail ?? string.Empty,
                    Code = EmployeeCode ?? CommonBaseConstant.AnonymousCode,
                    Name = EmployeeUserName ?? CommonBaseConstant.Anonymous,
                    Organization = Organization ?? CommonBaseConstant.OrganizationForISC,
                },

                Method = httpContext?.Request?.Method ?? string.Empty,
                Address = httpContext?.Request?.GetEncodedUrl() ?? string.Empty,
                Ip = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty,
                Device = httpContext?.Request?.Headers.UserAgent.ToString() ?? string.Empty
            };
        }

        #region :::::::::::::::::::::::::::::::::: Setup lấy thông tin Audit ::::::::::::::::::::::::::::::::::

        /// <summary>
        /// Gán thông tin audit của người dùng.
        /// </summary>
        /// <param name="audit"></param>
        /// <param name="employee"></param>
        /// <param name="defaultName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        protected static AuditModel SetAudit(AuditModel audit,
                                             GetAllEmployeeByFilterResponse employee,
                                             string defaultName = "",
                                             CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            audit ??= new();
                        
            switch (employee is not null)
            {
                case false:
                    {
                        audit = SetAuditDefaultWithOwner(audit: audit, defaultName: defaultName);

                        break;
                    }
                case true:
                    {
                        audit.CreatorInfo.Code = employee!.Code;
                        audit.CreatorInfo.Email = employee!.Email;
                        audit.CreatorInfo.Name = employee!.UserName;

                        audit.CreatorInfo.TitleCode = employee!.TitleCode;
                        audit.CreatorInfo.Organization = employee!.OrganizationCode;

                        audit.CreatorInfo.BranchId = employee!.InsideBranchId;
                        audit.CreatorInfo.LocationId = employee!.InsideLocationId;

                        break;
                    }
            }

            return audit;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="audit"></param>
        /// <param name="creatorInfo"></param>
        /// <param name="defaultName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ///
        protected static AuditModel SetAudit(AuditModel audit,
                                             CreatorInfo creatorInfo,
                                             string defaultName = "",
                                             CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            audit ??= new();
            
            switch (creatorInfo is not null)
            {
                case false:
                    {
                        audit = SetAuditDefaultWithOwner(audit: audit, defaultName: defaultName);

                        break;
                    }
                case true:
                    {
                        audit.CreatorInfo = creatorInfo;

                        break;
                    }
            }

            return audit;
        }

        /// <summary>
        /// Gán thông tin cơ bản của nhân sự theo cách gán mặc định.
        /// </summary>
        /// <param name="audit"></param>
        /// <param name="defaultName"></param>
        /// <returns></returns>
        ///
        private static AuditModel SetAuditDefaultWithOwner(AuditModel audit, string defaultName)
        {
            if (!string.IsNullOrWhiteSpace(defaultName))
            {
                audit.CreatorInfo.Name = defaultName;
            }
            else if (audit.CreatorInfo.Name.Equals(
                CommonBaseConstant.Anonymous, StringComparison.CurrentCultureIgnoreCase))
            {
                audit.CreatorInfo.Name = "SYSTEM-SR";
            }

            return audit;
        }

        #endregion :::::::::::::::::::::::::::::::::: Setup lấy thông tin Audit ::::::::::::::::::::::::::::::::::
    }
}