using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class UpsertAddressRequest
{
    public string? GooglePlaceId { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? StreetNumber { get; set; }
    public string? Route { get; set; }
    public string? SubLocality { get; set; }
    public string? Locality { get; set; }
    public string? AdministrativeAreaLevel1 { get; set; }
    public string? AdministrativeAreaLevel2 { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? PostalCode { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? PlusCode { get; set; }
}
