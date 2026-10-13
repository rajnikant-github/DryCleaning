using DryCleaning.Models;

namespace DryCleaning.Services;

public interface ISessionService
{
    UserRole SelectedRole { get; set; }
    User? CurrentUser { get; }

    /// <summary>Customer / Provider / Driver profile id of the signed-in user.</summary>
    Guid? ProfileId { get; }
    Guid? ProviderId { get; }
    Guid? CustomerId { get; }
    Guid? DriverId { get; }
    bool IsSignedIn { get; }
    void SignIn(User user, Guid? profileId);
    void SignOut();
}
