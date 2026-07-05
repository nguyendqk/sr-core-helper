using Microsoft.AspNetCore.Http;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.AddHeadersHttpClientExtensions
{
    public sealed class IPAddressForwardExtensions : DelegatingHandler
    {
        #region :::::::: Ctor ::::::::

        private readonly ILogger<IPAddressForwardExtensions> _logger;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public IPAddressForwardExtensions(
            ILogger<IPAddressForwardExtensions> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;

            _httpContextAccessor = httpContextAccessor;
        }

        #endregion :::::::: Ctor ::::::::

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                string ipAddress = string.Empty;

                string getIPAddress =
                    ConvertHelpers.GetClientIpAddress(_httpContextAccessor.HttpContext);

                if (!string.IsNullOrWhiteSpace(getIPAddress))
                {
                    ipAddress = getIPAddress;
                }

                request.Headers.Remove(HeaderConstant.ForwardedHeaderKey);

                request.Headers.TryAddWithoutValidation(HeaderConstant.ForwardedHeaderKey, ipAddress);
            }
            catch (Exception exception)
            {
                _logger.ErrorException(nameof(IPAddressForwardExtensions), nameof(SendAsync), e: exception);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}