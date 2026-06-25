using Microsoft.Extensions.DependencyInjection;

namespace FTELSRCore.Utilizes
{
    public class LazyInstanceUtility<T>(IServiceProvider serviceProvider) : Lazy<T>(serviceProvider.GetRequiredService<T>)
    { }
}