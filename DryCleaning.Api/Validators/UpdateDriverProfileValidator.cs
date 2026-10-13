using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class UpdateDriverProfileValidator : AbstractValidator<UpdateDriverProfileRequest>
{
    public UpdateDriverProfileValidator()
    {
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).MinimumLength(2).MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
