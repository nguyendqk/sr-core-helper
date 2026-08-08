using Microsoft.AspNetCore.Http;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.HeaderHttpClientExtensions
{
    public sealed class UserAgentForwardExtensions : DelegatingHandler
    {
        #region :::::::: Ctor ::::::::

        private readonly string _userAgent;

        private readonly ILogger<UserAgentForwardExtensions> _logger;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserAgentForwardExtensions(
            ILogger<UserAgentForwardExtensions> logger,
            string userAgent,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;

            _userAgent = userAgent;

            _httpContextAccessor = httpContextAccessor;
        }

        #endregion :::::::: Ctor ::::::::

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string userAgent;

                string getUserAgent =
                    ConvertHelpers.GetUserAgent(_httpContextAccessor.HttpContext);

                if (!string.IsNullOrWhiteSpace(getUserAgent))
                {
                    userAgent = getUserAgent;
                }
                else
                {
                    userAgent = _userAgent;
                }

                request.Headers.Remove(HeaderConstant.UserAgentHeaderKey);

                request.Headers.TryAddWithoutValidation(HeaderConstant.UserAgentHeaderKey, userAgent);
            }
            catch (Exception exception)
            {
                _logger.ErrorException(nameof(UserAgentForwardExtensions), nameof(SendAsync), e: exception);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}