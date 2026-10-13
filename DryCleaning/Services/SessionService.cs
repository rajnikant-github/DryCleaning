using DryCleaning.Models;

namespace DryCleaning.Services;

public sealed class SessionService : ISessionService
{
    public UserRole SelectedRole { get; set; } = UserRole.Customer;
    public User? CurrentUser { get; private set; }
    public Guid? ProfileId { get; private set; }
    public Guid? ProviderId => CurrentUser?.Role == UserRole.Provider ? ProfileId : null;
    public Guid? CustomerId => CurrentUser?.Role == UserRole.Customer ? ProfileId : null;
    public Guid? DriverId => CurrentUser?.Role == UserRole.Driver ? ProfileId : null;
    public bool IsSignedIn => CurrentUser is not null;

    public void SignIn(User user, Guid? profileId)
    {
        CurrentUser = user;
        ProfileId = profileId;
        SelectedRole = user.Role;
    }

    public void SignOut()
    {
        CurrentUser = null;
        ProfileId = null;
    }
}
