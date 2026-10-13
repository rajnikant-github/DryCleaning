using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class BadRequestException(string message) : AppException(message, StatusCodes.Status400BadRequest);
