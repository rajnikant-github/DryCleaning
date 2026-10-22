namespace DryCleaning.Services.Api.Contracts;

/// <summary>PUT api/customer/orders/{id}/driver-notes. Empty clears the note.</summary>
public sealed class DriverNotesRequest
{
    public string? Notes { get; set; }
}
