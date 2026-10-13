using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class ReceiveOrderValidator : AbstractValidator<ReceiveOrderRequest>
{
    public ReceiveOrderValidator()
    {
        RuleFor(x => x.ActualItemCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(400);
    }
}
