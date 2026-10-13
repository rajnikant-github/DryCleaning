using System.Net;

namespace DryCleaning.Services.Api;

/// <summary>An API call failed. <see cref="Exception.Message"/> is safe to show to the user.</summary>
public sealed class ApiException(string message, HttpStatusCode? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}
