using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{4,8}$");
        RuleFor(x => x.NewPassword).Password();
    }
}
