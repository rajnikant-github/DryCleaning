using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpRequest>
{
    public RequestOtpValidator() => RuleFor(x => x.Identifier).NotEmpty().EmailAddress().WithMessage("Enter the email address on your account.").MaximumLength(100);
}
