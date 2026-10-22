using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class SubmitRatingValidator : AbstractValidator<SubmitRatingRequest>
{
    public SubmitRatingValidator()
    {
        RuleFor(x => x.Stars).InclusiveBetween(1, 5).WithMessage("Choose 1 to 5 stars.");
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}
