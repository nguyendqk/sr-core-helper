using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace FTELSRCore.Infrastructure.Extensions.Helpers.SwaggerExtensions
{
    public class SwaggerExtensions(IApiVersionDescriptionProvider provider, string userAgent) : IConfigureOptions<SwaggerGenOptions>
    {
        private const string RoutePrefixToken = "Bearer";

        private readonly string _userAgent = userAgent;

        private readonly OpenApiSecurityScheme _securityScheme = new()
        {
            Name = "Authorization",
            Scheme = RoutePrefixToken,
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        };

        public void Configure(SwaggerGenOptions options)
        {
            // Include doc version
            foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc($"v{description.ApiVersion}_{description.GroupName}", CreateInfoForApiVersion(description));
            }

            // Include all project's xml comments
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic)
                {
                    continue;
                }

                string xmlFile = $"{assembly.GetName().Name}.xml";
                string xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, xmlFile);

                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            }

            options.AddSecurityRequirement(document => new()
            {
                [new OpenApiSecuritySchemeReference(RoutePrefixToken, document)] = []
            });

            options.AddSecurityDefinition(RoutePrefixToken, _securityScheme);

            // Include Description
            options.EnableAnnotations();
        }

        private OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            OpenApiInfo info = new()
            {
                Version = description.ApiVersion.ToString(),
                Title = $"{_userAgent} - {description.GroupName.ToUpper()}",
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new(uriString: "https://opensource.org/licenses/MIT")
                }
            };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}