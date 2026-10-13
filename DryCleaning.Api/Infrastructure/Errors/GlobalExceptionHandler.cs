using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Infrastructure.Errors;

/// <summary>Maps exceptions to RFC 7807 ProblemDetails responses.</summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            AppException app => (app.StatusCode, app.Message),
            DomainException domain => (StatusCodes.Status409Conflict, domain.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was changed by someone else. Please refresh and try again."),
            DbUpdateException => (StatusCodes.Status409Conflict, "The change conflicts with existing data."),
            OperationCanceledException => (499, "Request cancelled."),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong.")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("Request failed ({Status}): {Message}", status, exception.Message);

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }
}
