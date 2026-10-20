using System.Net;

namespace DryCleaning.Admin.Services;

/// <summary>An error answer from the API, with a message that is safe to show to the admin.</summary>
public sealed class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
    public bool IsUnauthorized => Status == HttpStatusCode.Unauthorized;
}
