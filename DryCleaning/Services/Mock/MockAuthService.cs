using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockAuthService(MockDataStore store, ISessionService session) : IAuthService
{
    /// <summary>Demo OTP accepted by the mock service.</summary>
    public const string DemoOtp = "123456";

    public async Task<AuthResult> LoginAsync(string identifier, string password, UserRole role)
    {
        await MockDataStore.SimulateLatencyAsync();

        if (string.IsNullOrWhiteSpace(identifier))
            return AuthResult.Fail("Enter your mobile number or email.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            return AuthResult.Fail("Password must be at least 4 characters.");

        return SignInAs(role);
    }

    public async Task<string> RequestOtpAsync(string identifier, UserRole role)
    {
        await MockDataStore.SimulateLatencyAsync();
        var digits = new string(identifier.Where(char.IsDigit).ToArray());
        if (identifier.Contains('@'))
        {
            var at = identifier.IndexOf('@');
            return $"{identifier[..Math.Min(2, at)]}***{identifier[at..]}";
        }

        return digits.Length >= 4 ? $"+27 ** *** {digits[^4..]}" : identifier;
    }

    public async Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role)
    {
        await MockDataStore.SimulateLatencyAsync();
        return code == DemoOtp
            ? SignInAs(role)
            : AuthResult.Fail("That code is incorrect. Please try again.");
    }

    public string? OtpHint => DemoOtp;

    public async Task<AuthResult> RegisterAsync(Registration registration)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var email = registration.Email?.Trim().ToLowerInvariant();
            var phone = registration.Phone?.Trim();
            if ((!string.IsNullOrEmpty(email) && store.Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(phone) && store.Users.Any(u => u.Phone == phone)))
                return AuthResult.Fail("An account with that email or mobile number already exists.");

            var user = new User
            {
                Role = registration.Role,
                Name = registration.Name.Trim(),
                Email = email ?? string.Empty,
                Phone = phone ?? string.Empty,
                PasswordHash = "mock"
            };
            store.Users.Add(user);

            switch (registration.Role)
            {
                case UserRole.Customer:
                    var customer = new Customer { UserId = user.Id };
                    store.Customers.Add(customer);
                    session.SignIn(user, customer.Id);
                    return AuthResult.Ok();

                case UserRole.Driver:
                    var driver = new Driver { UserId = user.Id, LicenseNumber = registration.LicenseNumber?.Trim() ?? string.Empty, IsAvailable = false };
                    store.Drivers.Add(driver);
                    session.SignIn(user, driver.Id);
                    return AuthResult.Ok();

                case UserRole.Provider:
                    user.IsActive = false;
                    var provider = new Provider
                    {
                        UserId = user.Id,
                        BusinessName = registration.BusinessName?.Trim() ?? string.Empty,
                        Description = registration.Description?.Trim() ?? string.Empty,
                        Phone = user.Phone,
                        Email = user.Email,
                        IsActive = false
                    };
                    store.Providers.Add(provider);
                    foreach (var categoryId in registration.CategoryIds.Distinct())
                        store.ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = provider.Id, CategoryId = categoryId });
                    return AuthResult.Pending(PendingApprovalMessage);

                default:
                    return AuthResult.Fail("Only Customer, Service Provider and Driver accounts can be created in the app.");
            }
        }
    }

    public Task<string> RequestPasswordResetAsync(string identifier) => RequestOtpAsync(identifier, session.SelectedRole);

    public async Task<AuthResult> ResetPasswordAsync(string identifier, string code, string newPassword, UserRole role)
    {
        await MockDataStore.SimulateLatencyAsync();
        return code == DemoOtp
            ? SignInAs(role)
            : AuthResult.Fail("That code is incorrect or has expired. Request a new one.");
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        await MockDataStore.SimulateLatencyAsync();
        if (string.IsNullOrEmpty(currentPassword))
            throw new InvalidOperationException("Enter your current password.");
        if (currentPassword == newPassword)
            throw new InvalidOperationException("Choose a different password.");
    }

    /// <summary>Shown after a Service Provider signs up (the account needs admin approval).</summary>
    public const string PendingApprovalMessage =
        "Thanks for registering! Your business is being reviewed by the E Dry Cleaners team. We'll let you know as soon as it's approved – then you can sign in.";

    public Task<bool> RestoreSessionAsync() => Task.FromResult(session.IsSignedIn);

    public Task LogoutAsync()
    {
        session.SignOut();
        return Task.CompletedTask;
    }

    private AuthResult SignInAs(UserRole role)
    {
        switch (role)
        {
            case UserRole.Provider:
                session.SignIn(store.DemoProviderUser, store.DemoProvider.Id);
                return AuthResult.Ok();
            case UserRole.Customer:
                session.SignIn(store.DemoCustomerUser, store.DemoCustomer.Id);
                return AuthResult.Ok();
            case UserRole.Driver:
                session.SignIn(store.DemoDriverUser, store.DemoDriver.Id);
                return AuthResult.Ok();
            default:
                return AuthResult.Fail($"The {role} app is not available in this build yet.");
        }
    }
}
