using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class VerifyCodeValidator : AbstractValidator<VerifyCodeRequest>
{
    public VerifyCodeValidator()
    {
        RuleFor(x => x.Code).NotNull().MaximumLength(10);
        RuleFor(x => x.ItemCount).GreaterThanOrEqualTo(0).When(x => x.ItemCount is not null);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}
