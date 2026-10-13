using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class ReasonValidator : AbstractValidator<ReasonRequest>
{
    public ReasonValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(400);
}
