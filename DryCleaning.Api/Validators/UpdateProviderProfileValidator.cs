using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class UpdateProviderProfileValidator : AbstractValidator<UpdateProviderProfileRequest>
{
    public UpdateProviderProfileValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.LogoUrl).MaximumLength(255);
    }
}
