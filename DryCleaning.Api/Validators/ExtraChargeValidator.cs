using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class ExtraChargeValidator : AbstractValidator<ExtraChargeRequest>
{
    public ExtraChargeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(200);
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 100000m).WithMessage("The amount must be more than zero.");
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 1000);
    }
}
