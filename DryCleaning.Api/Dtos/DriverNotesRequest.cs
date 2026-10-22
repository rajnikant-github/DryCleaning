namespace DryCleaning.Api.Dtos;

public sealed class DriverNotesRequest
{
    /// <summary>The note for the driver. Empty clears it.</summary>
    public string? Notes { get; set; }
}
