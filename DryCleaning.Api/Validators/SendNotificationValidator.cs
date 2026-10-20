using DryCleaning.Api.Dtos;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.Audience).IsInEnum();
        RuleFor(x => x.UserId).NotEmpty().When(x => x.Audience == NotificationAudience.User)
            .WithMessage("Choose the user to notify.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(500);
    }
}
