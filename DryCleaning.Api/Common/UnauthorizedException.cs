using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class UnauthorizedException(string message) : AppException(message, StatusCodes.Status401Unauthorized);
