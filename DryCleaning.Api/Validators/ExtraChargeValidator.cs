using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class ExtraChargeValidator : AbstractValidator<ExtraChargeRequest>
{
    public ExtraChargeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.PercentBase).IsInEnum();
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 100000m).When(x => x.Type == ExtraChargeType.Fixed)
            .WithMessage("The amount must be more than zero.");
        RuleFor(x => x.Percent).InclusiveBetween(0.01m, 100m).When(x => x.Type == ExtraChargeType.Percentage)
            .WithMessage("The percentage must be between 0.01 and 100.");
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 1000);
    }
}
