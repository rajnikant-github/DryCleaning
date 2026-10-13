using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{4,8}$");
    }
}
