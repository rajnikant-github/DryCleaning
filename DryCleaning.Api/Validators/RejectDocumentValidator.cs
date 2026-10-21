using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RejectDocumentValidator : AbstractValidator<RejectDocumentRequest>
{
    public RejectDocumentValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Tell the applicant why the document was rejected.").MaximumLength(500);
    }
}
