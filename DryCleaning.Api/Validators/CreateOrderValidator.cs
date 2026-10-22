using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ServiceProviderId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Items.Count).LessThanOrEqualTo(50).WithMessage("An order can have at most 50 lines.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServiceItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 100);
            item.RuleFor(i => i.Notes).MaximumLength(500);
        });
        RuleFor(x => x.PickupDate)
            .Must(d => d is null || d.Value >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithMessage("Pickup date cannot be in the past.")
            .Must(d => d is null || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))
            .WithMessage("Pickup can be scheduled up to 30 days ahead.");
        RuleFor(x => x.PickupSlot).MaximumLength(50);
        RuleFor(x => x.DriverNotes).MaximumLength(300);
        RuleFor(x => x.PickupSlot).NotEmpty().When(x => x.PickupDate is not null)
            .WithMessage("Choose a pickup time slot.");
    }
}
