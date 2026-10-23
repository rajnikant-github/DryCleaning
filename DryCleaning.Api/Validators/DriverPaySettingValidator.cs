using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class DriverPaySettingValidator : AbstractValidator<DriverPaySettingDto>
{
    public DriverPaySettingValidator()
    {
        RuleFor(x => x.PayPerJob).InclusiveBetween(0m, 100000m).WithMessage("The pay per job must be between 0 and 100000.");
    }
}
