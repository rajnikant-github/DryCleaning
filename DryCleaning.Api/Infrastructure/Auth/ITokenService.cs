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

public interface ITokenService
{
    /// <summary>Creates the short-lived JWT access token (no refresh token – see AuthService.IssueAsync).</summary>
    AuthResponse Create(User user, Guid? profileId);

    /// <summary>A new random refresh token and its SHA-256 hash (only the hash is stored).</summary>
    (string Token, string Hash) NewRefreshToken();

    string HashRefreshToken(string token);
}
