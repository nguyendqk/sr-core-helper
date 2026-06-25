using Microsoft.AspNetCore.Routing;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.SlugifyParameterTransformerExtensions
{
    public partial class SlugifyParameterTransformerExtensions : IOutboundParameterTransformer
    {
        public string TransformOutbound(object value)
        {
            // Slugify value
            return value == null ? null : RegexController().Replace(value.ToString() ?? string.Empty, "$1-$2").ToLower(CultureInfo.CurrentCulture);
        }

        [GeneratedRegex("([a-z])([A-Z])")]
        private static partial Regex RegexController();
    }
}