using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class ForbiddenException(string message) : AppException(message, StatusCodes.Status403Forbidden);
