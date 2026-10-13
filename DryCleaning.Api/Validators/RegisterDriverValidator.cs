using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RegisterDriverValidator : AbstractValidator<RegisterDriverRequest>
{
    public RegisterDriverValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).NotEmpty().Phone();
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
    }
}
