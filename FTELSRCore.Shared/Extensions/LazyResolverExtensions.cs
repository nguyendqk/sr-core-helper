using Microsoft.Extensions.DependencyInjection;

namespace FTELSRCore.Extensions
{
    public class LazyResolverExtensions
    {
        public class LazyResolver<T>(IServiceProvider provider) : Lazy<T>(provider.GetRequiredService<T>)
        { }
    }
}