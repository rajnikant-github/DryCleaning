using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class VatSettingValidator : AbstractValidator<VatSettingDto>
{
    public VatSettingValidator()
    {
        RuleFor(x => x.Rate).InclusiveBetween(0m, 100m).WithMessage("The VAT rate must be between 0 and 100%.");
    }
}
