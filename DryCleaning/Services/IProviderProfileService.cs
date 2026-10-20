using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IProviderProfileService
{
    Task<ProviderProfile> GetProfileAsync();

    /// <summary>Saves business name, description, phone and email.</summary>
    Task UpdateProfileAsync(ProviderProfileUpdate update);

    /// <summary>Saves the business address (from the address picker) and returns it as stored.</summary>
    Task<Address> SaveAddressAsync(Address address);

    /// <summary>Uploads a new logo from a local photo and returns its URL.</summary>
    Task<string> UploadLogoAsync(string localPath);

    /// <summary>All active marketplace categories, marking the ones this provider offers.</summary>
    Task<IReadOnlyList<ProviderCategoryOption>> GetCategoryOptionsAsync();

    /// <summary>Sets which categories this provider offers (at least one).</summary>
    Task SetCategoriesAsync(IReadOnlyList<Guid> categoryIds);

    /// <summary>My pickup days and hours: seven entries, Sunday first.</summary>
    Task<IReadOnlyList<WorkingHour>> GetWorkingHoursAsync();

    /// <summary>Saves my pickup days and hours (all seven days; at least one open).</summary>
    Task SetWorkingHoursAsync(IReadOnlyList<WorkingHour> hours);
}
