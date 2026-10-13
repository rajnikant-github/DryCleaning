using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);
