using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using FluentValidation;

namespace DryCleaning.Api.Validators;

public sealed class SetWorkingHoursValidator : AbstractValidator<SetWorkingHoursRequest>
{
    public SetWorkingHoursValidator()
    {
        RuleFor(x => x.Days).Must(d => d.Count == 7 && d.Select(x => x.Day).Distinct().Count() == 7)
            .WithMessage("Send all seven days, once each.");
        RuleFor(x => x.Days).Must(d => d.Any(x => x.IsOpen)).WithMessage("Be open on at least one day.");
        RuleForEach(x => x.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.Day).InclusiveBetween(0, 6);
            day.RuleFor(d => d).Must(d => !d.IsOpen || WorkingHoursService.IsValidWindow(d.OpenTime, d.CloseTime))
                .WithMessage("Open days need an opening time before the closing time, at least two hours apart (HH:mm).");
        });
    }
}
