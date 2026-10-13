using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class InspectionValidator : AbstractValidator<InspectionRequest>
{
    public InspectionValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.OrderItemId).NotEmpty();
            item.RuleFor(i => i.ReceivedQuantity).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.Note).MaximumLength(200);
        });
    }
}
