namespace DryCleaning.Admin.Models;

public sealed class AddressDto
{
    public Guid Id { get; set; }
    public string? GooglePlaceId { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
    public string? Locality { get; set; }
    public string? PostalCode { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
