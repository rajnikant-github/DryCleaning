using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RegisterResendValidator : AbstractValidator<RegisterResendRequest>
{
    public RegisterResendValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
