using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Phone().When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithName("Email").WithMessage("A mobile number or email is required.");
        RuleFor(x => x.Password).Password();
    }
}
