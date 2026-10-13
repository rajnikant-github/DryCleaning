using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class DriverPickupValidator : AbstractValidator<DriverPickupRequest>
{
    public DriverPickupValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ItemCount).GreaterThanOrEqualTo(0).When(x => x.ItemCount is not null);
        RuleFor(x => x.Notes).MaximumLength(300);
        RuleFor(x => x.Items!.Count).LessThanOrEqualTo(50).When(x => x.Items is not null);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.OrderItemId).NotEmpty();
            item.RuleFor(i => i.CountedQuantity).InclusiveBetween(0, 200);
            item.RuleFor(i => i.Condition).IsInEnum();
            item.RuleFor(i => i.Note).MaximumLength(200);
        });
    }
}
