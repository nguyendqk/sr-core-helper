using FTELSRCore.Infrastructure.MiddleWares.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FTELSRCore.Infrastructure.MiddleWares
{
    public sealed class ResponseFTELCoreWrapperFilter(ResponseFTELCoreWrapperModel wrapperModel) : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(
            ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult { Value: IResult result } _ && result.Meta is null)
            {
                result.System = wrapperModel?.ServiceName ?? CommonBaseConstant.System;

                result.Meta = BuildMetaHelper.Build(context.HttpContext);
            }

            await next();
        }
    }

    public record ResponseFTELCoreWrapperModel
    {
        public string ServiceName { get; set; }
    }
}