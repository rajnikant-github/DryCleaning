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

    private readonly Dictionary<string, Registration> _pending = new(StringComparer.OrdinalIgnoreCase);

    public async Task<RegistrationStart> StartRegistrationAsync(Registration registration)
    {
        await MockDataStore.SimulateLatencyAsync();
        var email = registration.Email?.Trim().ToLowerInvariant();
        var phone = registration.Phone?.Trim();
        if (string.IsNullOrEmpty(email))
            return RegistrationStart.Fail("An email address is required.");
        lock (store.Gate)
        {
            if (store.Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(phone) && store.Users.Any(u => u.Phone == phone)))
                return RegistrationStart.Fail("An account with that email or mobile number already exists.");
        }

        _pending[email] = registration;
        return RegistrationStart.Ok(MaskEmail(email));
    }

    public async Task<RegistrationStart> ResendRegistrationCodeAsync(string email)
    {
        await MockDataStore.SimulateLatencyAsync();
        return _pending.ContainsKey(email.Trim())
            ? RegistrationStart.Ok(MaskEmail(email.Trim().ToLowerInvariant()))
            : RegistrationStart.Fail("We couldn't find that sign-up. Please start again.");
    }

    public async Task<AuthResult> ConfirmRegistrationAsync(string email, string code)
    {
        await MockDataStore.SimulateLatencyAsync();
        if (!_pending.TryGetValue(email.Trim(), out var registration))
            return AuthResult.Fail("We couldn't find that sign-up. Please start again.");
        if (code.Trim() != DemoOtp)
            return AuthResult.Fail("That code is incorrect. Please try again.");

        _pending.Remove(email.Trim());
        return CreateAccount(registration);
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at < 0 ? email : $"{email[..Math.Min(2, at)]}***{email[at..]}";
    }

    private AuthResult CreateAccount(Registration registration)
    {
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
                    user.ProfileStatus = ProfileStatus.PendingApproval; // demo: approved on the first "Check status"
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
                    user.ProfileStatus = ProfileStatus.PendingApproval; // demo: approved on the first "Check status"
                    session.SignIn(user, provider.Id);
                    return AuthResult.Ok();

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

    /// <summary>Demo data has no admin, so the first status check "approves" a pending account.</summary>
    public async Task<User?> RefreshAccountAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        if (session.CurrentUser is { } user && user.ProfileStatus != ProfileStatus.Active)
        {
            user.ProfileStatus = ProfileStatus.Active;
            user.IsActive = true;
        }

        return session.CurrentUser;
    }

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
