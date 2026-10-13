using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class StartDeliveryValidator : AbstractValidator<StartDeliveryRequest>
{
    public StartDeliveryValidator()
    {
        RuleFor(x => x.PackageCount).InclusiveBetween(1, 50).When(x => x.PackageCount is not null);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}
