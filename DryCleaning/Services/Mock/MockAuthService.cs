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
