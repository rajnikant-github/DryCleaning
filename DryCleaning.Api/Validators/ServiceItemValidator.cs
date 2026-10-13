using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class ServiceItemValidator : AbstractValidator<ServiceItemRequest>
{
    public ServiceItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ImageUrl).MaximumLength(255);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(100_000_000m);
        RuleFor(x => x.EstimatedDays).InclusiveBetween(1, 60);
    }
}
