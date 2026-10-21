using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class TestEmailValidator : AbstractValidator<TestEmailRequest>
{
    public TestEmailValidator()
    {
        RuleFor(x => x.To).NotEmpty().EmailAddress().MaximumLength(200);
    }
}
