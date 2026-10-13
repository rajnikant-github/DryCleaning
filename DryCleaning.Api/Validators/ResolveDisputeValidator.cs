using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class ResolveDisputeValidator : AbstractValidator<ResolveDisputeRequest>
{
    public ResolveDisputeValidator()
    {
        RuleFor(x => x.Status).Must(s => s is DisputeStatus.Resolved or DisputeStatus.Rejected)
            .WithMessage("Status must be Resolved or Rejected.");
        RuleFor(x => x.Resolution).NotEmpty().MaximumLength(1000);
    }
}
