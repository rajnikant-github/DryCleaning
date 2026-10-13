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

public sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerRequest>
{
    public RegisterCustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Phone().When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithName("Email").WithMessage("A mobile number or email is required.");
        RuleFor(x => x.Password).Password();
    }
}

public sealed class RegisterProviderValidator : AbstractValidator<RegisterProviderRequest>
{
    public RegisterProviderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Phone().When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithName("Email").WithMessage("A mobile number or email is required.");
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class RegisterDriverValidator : AbstractValidator<RegisterDriverRequest>
{
    public RegisterDriverValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).NotEmpty().Phone();
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpRequest>
{
    public RequestOtpValidator() => RuleFor(x => x.Identifier).NotEmpty().MaximumLength(100);
}

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Identifier).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{4,8}$");
    }
}

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).Password().NotEqual(x => x.CurrentPassword).WithMessage("Choose a different password.");
    }
}

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Phone().When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

public sealed class AddressValidator : AbstractValidator<UpsertAddressRequest>
{
    public AddressValidator()
    {
        RuleFor(x => x.FormattedAddress).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.GooglePlaceId).MaximumLength(255);
        RuleFor(x => x.AddressLine1).MaximumLength(255);
        RuleFor(x => x.AddressLine2).MaximumLength(255);
        RuleFor(x => x.StreetNumber).MaximumLength(50);
        RuleFor(x => x.Route).MaximumLength(255);
        RuleFor(x => x.SubLocality).MaximumLength(255);
        RuleFor(x => x.Locality).MaximumLength(255);
        RuleFor(x => x.AdministrativeAreaLevel1).MaximumLength(255);
        RuleFor(x => x.AdministrativeAreaLevel2).MaximumLength(255);
        RuleFor(x => x.Country).MaximumLength(255);
        RuleFor(x => x.CountryCode).MaximumLength(10);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.PlusCode).MaximumLength(255);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}

public sealed class CategoryValidator : AbstractValidator<CategoryRequest>
{
    public CategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ImageUrl).MaximumLength(255);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class ProductValidator : AbstractValidator<ProductRequest>
{
    public ProductValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ImageUrl).MaximumLength(255);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder is not null);
    }
}

public sealed class ServiceItemValidator : AbstractValidator<ServiceItemRequest>
{
    public ServiceItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ImageUrl).MaximumLength(255);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(100_000_000m);
        RuleFor(x => x.EstimatedDays).InclusiveBetween(1, 60);
    }
}

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

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ServiceProviderId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one item.");
        RuleFor(x => x.Items.Count).LessThanOrEqualTo(50).WithMessage("An order can have at most 50 lines.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ServiceItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 100);
            item.RuleFor(i => i.Notes).MaximumLength(500);
        });
        RuleFor(x => x.PickupDate)
            .Must(d => d is null || d.Value >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithMessage("Pickup date cannot be in the past.")
            .Must(d => d is null || d.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)))
            .WithMessage("Pickup can be scheduled up to 30 days ahead.");
        RuleFor(x => x.PickupSlot).MaximumLength(50);
        RuleFor(x => x.PickupSlot).NotEmpty().When(x => x.PickupDate is not null)
            .WithMessage("Choose a pickup time slot.");
    }
}

public sealed class ReasonValidator : AbstractValidator<ReasonRequest>
{
    public ReasonValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(400);
}

public sealed class IssueValidator : AbstractValidator<IssueRequest>
{
    public IssueValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class ReceiveOrderValidator : AbstractValidator<ReceiveOrderRequest>
{
    public ReceiveOrderValidator()
    {
        RuleFor(x => x.ActualItemCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(400);
    }
}

public sealed class InspectionValidator : AbstractValidator<InspectionRequest>
{
    public InspectionValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.OrderItemId).NotEmpty();
            item.RuleFor(i => i.ReceivedQuantity).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.Note).MaximumLength(200);
        });
    }
}

public sealed class VerifyCodeValidator : AbstractValidator<VerifyCodeRequest>
{
    public VerifyCodeValidator()
    {
        RuleFor(x => x.Code).NotNull().MaximumLength(10);
        RuleFor(x => x.ItemCount).GreaterThanOrEqualTo(0).When(x => x.ItemCount is not null);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}

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

public sealed class ResolveDisputeValidator : AbstractValidator<ResolveDisputeRequest>
{
    public ResolveDisputeValidator()
    {
        RuleFor(x => x.Status).Must(s => s is DisputeStatus.Resolved or DisputeStatus.Rejected)
            .WithMessage("Status must be Resolved or Rejected.");
        RuleFor(x => x.Resolution).NotEmpty().MaximumLength(1000);
    }
}
