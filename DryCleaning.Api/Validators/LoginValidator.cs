using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().EmailAddress().WithMessage("Enter the email address on your account.").MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}
