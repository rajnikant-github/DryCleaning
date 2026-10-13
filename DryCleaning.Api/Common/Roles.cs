using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public static class Roles
{
    public const string Customer = nameof(UserRole.Customer);
    public const string Provider = nameof(UserRole.Provider);
    public const string Driver = nameof(UserRole.Driver);
    public const string Admin = nameof(UserRole.Admin);
}
