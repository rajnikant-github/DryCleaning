using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class DeliveryPricingValidator : AbstractValidator<DeliveryPricingDto>
{
    public DeliveryPricingValidator()
    {
        RuleFor(x => x.BaseFee).InclusiveBetween(0, 10000);
        RuleFor(x => x.PricePerKm).InclusiveBetween(0.01m, 1000m).WithMessage("Pickup & delivery is charged per kilometre: the price per km must be more than zero.");
        RuleFor(x => x.IncludedKm).InclusiveBetween(0, 1000);
        RuleFor(x => x.MinFee).InclusiveBetween(0, 10000);
        RuleFor(x => x.MaxFee).InclusiveBetween(0, 10000).When(x => x.MaxFee is not null);
        RuleFor(x => x.MaxFee).GreaterThanOrEqualTo(x => x.MinFee).When(x => x.MaxFee is not null)
            .WithMessage("The maximum fee can't be lower than the minimum fee.");
    }
}
