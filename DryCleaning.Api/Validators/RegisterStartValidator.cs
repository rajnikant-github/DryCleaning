using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RegisterStartValidator : AbstractValidator<RegisterStartRequest>
{
    public RegisterStartValidator()
    {
        RuleFor(x => x.Role).Must(r => r is UserRole.Customer or UserRole.Provider or UserRole.Driver)
            .WithMessage("Only customer, service provider and driver accounts can be created here.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100).WithMessage("A valid email address is required.");
        RuleFor(x => x.Phone).Phone().When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Password).Password();

        When(x => x.Role == UserRole.Provider, () =>
        {
            RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
        });
        When(x => x.Role == UserRole.Driver, () =>
            RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50));
    }
}
