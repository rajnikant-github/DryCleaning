using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class AddressValidator : AbstractValidator<UpsertAddressRequest>
{
    public AddressValidator()
    {
        RuleFor(x => x.FormattedAddress).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.GooglePlaceId).MaximumLength(255);
        RuleFor(x => x.AddressLine1).MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.StreetNumber).MaximumLength(50);
        RuleFor(x => x.Route).MaximumLength(255);
        RuleFor(x => x.SubLocality).MaximumLength(255);
        RuleFor(x => x.Locality).MaximumLength(255);
        RuleFor(x => x.AdministrativeAreaLevel1).MaximumLength(255);
        RuleFor(x => x.AdministrativeAreaLevel2).MaximumLength(255);
        RuleFor(x => x.Country).MaximumLength(255);
        RuleFor(x => x.CountryCode).MaximumLength(10);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.PlusCode).MaximumLength(255);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
