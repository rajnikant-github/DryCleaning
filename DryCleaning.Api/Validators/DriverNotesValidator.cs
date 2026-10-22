using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class DriverNotesValidator : AbstractValidator<DriverNotesRequest>
{
    public DriverNotesValidator() => RuleFor(x => x.Notes).MaximumLength(300);
}
