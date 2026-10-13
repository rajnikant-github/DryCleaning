using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RegisterDeviceValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Platform).IsInEnum();
    }
}
