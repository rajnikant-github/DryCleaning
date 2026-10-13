using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Storage;

public interface IFileStorage
{
    /// <summary>Saves the stream and returns a public relative URL (e.g. /uploads/orders/…/x.jpg).</summary>
    Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct);
}
