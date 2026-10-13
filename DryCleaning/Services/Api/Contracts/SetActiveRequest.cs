using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>PUT .../active and driver availability.</summary>
public sealed class SetActiveRequest
{
    public bool IsActive { get; set; }
}
