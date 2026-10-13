using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

// Request validation (runs automatically via ValidationFilter). Lengths match the schema.

internal static class Rules
{
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(8).MaximumLength(100)
            .Matches("[A-Za-z]").WithMessage("Password must contain a letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.");

    public static IRuleBuilderOptions<T, string?> Phone<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Matches(@"^\+?[0-9 ()-]{9,20}$").WithMessage("Enter a valid mobile number.");
}
