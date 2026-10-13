using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class ConflictException(string message) : AppException(message, StatusCodes.Status409Conflict);
