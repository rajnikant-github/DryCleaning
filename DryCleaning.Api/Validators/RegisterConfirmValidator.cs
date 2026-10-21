using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RegisterConfirmValidator : AbstractValidator<RegisterConfirmRequest>
{
    public RegisterConfirmValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(4, 10);
    }
}
