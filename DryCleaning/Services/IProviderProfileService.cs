using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IProviderProfileService
{
    Task<ProviderProfile> GetProfileAsync();
}
