using DryCleaning.Api.Common;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>
/// Sets the request-size cap for a verification document upload from the larger of <c>Verification:MaxPdfBytes</c> and <c>Verification:MaxImageBytes</c> (plus a little for the form
/// fields), so those settings are the only thing to change. It runs before the form is read, so an oversized upload is cut off early.
/// </summary>
public sealed class VerificationUploadLimitFilter(IOptions<VerificationOptions> options) : IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } feature)
            feature.MaxRequestBodySize = Math.Max(options.Value.MaxPdfBytes, options.Value.MaxImageBytes) + 1024 * 1024;
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
