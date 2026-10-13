using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Address (Google Places + normalized fields)

public sealed class AddressDto
{
    public required Guid Id { get; init; }
    public required string? GooglePlaceId { get; init; }
    public required string FormattedAddress { get; init; }
    public required string? AddressLine1 { get; init; }
    public required string? AddressLine2 { get; init; }
    public required string? StreetNumber { get; init; }
    public required string? Route { get; init; }
    public required string? SubLocality { get; init; }
    public required string? Locality { get; init; }
    public required string? AdministrativeAreaLevel1 { get; init; }
    public required string? AdministrativeAreaLevel2 { get; init; }
    public required string? Country { get; init; }
    public required string? CountryCode { get; init; }
    public required string? PostalCode { get; init; }
    public required decimal Latitude { get; init; }
    public required decimal Longitude { get; init; }
    public required string? PlusCode { get; init; }
}
