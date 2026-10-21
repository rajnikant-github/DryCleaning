using System.Text.RegularExpressions;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class DocumentTypeValidator : AbstractValidator<DocumentTypeRequest>
{
    public DocumentTypeValidator()
    {
        RuleFor(x => x.Role).Must(r => r is UserRole.Provider or UserRole.Driver).WithMessage("Documents are for providers or drivers.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.NumberLabel).MaximumLength(60);
        RuleFor(x => x.NumberPattern).MaximumLength(200)
            .Must(IsValidPattern).WithMessage("That is not a valid pattern (regular expression).")
            .When(x => !string.IsNullOrWhiteSpace(x.NumberPattern));
        RuleFor(x => x.NumberPattern).Empty().When(x => string.IsNullOrWhiteSpace(x.NumberLabel))
            .WithMessage("A number pattern needs a number label.");
        RuleFor(x => x.DisplayOrder).InclusiveBetween(0, 1000);
    }

    private static bool IsValidPattern(string? pattern)
    {
        try
        {
            _ = new Regex(pattern!, RegexOptions.None, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
