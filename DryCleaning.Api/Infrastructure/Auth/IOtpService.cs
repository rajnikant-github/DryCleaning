using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DryCleaning.Api.Infrastructure.Auth;

public interface IOtpService
{
    /// <summary>Generates, stores and sends a code. Returns the code (for dev exposure only).</summary>
    Task<string> SendAsync(string key, string destination, CancellationToken ct);
    bool Verify(string key, string code);
}
