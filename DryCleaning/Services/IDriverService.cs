using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IDriverService
{
    Task<DriverDashboard> GetDashboardAsync();
    Task<bool> SetOnlineAsync(bool isOnline);

    Task<IReadOnlyList<DriverJobSummary>> GetJobsAsync(DriverJobFilter filter);
    Task<IReadOnlyDictionary<DriverJobFilter, int>> GetJobCountsAsync();
    Task<DriverJobDetail> GetJobAsync(Guid jobId);

    /// <summary>The job a notification about this order refers to (own job first, then a pending one).</summary>
    Task<Guid?> FindJobForOrderAsync(Guid orderId);

    /// <summary>D03 – take a Pending job (one active job at a time, must be online).</summary>
    Task AcceptJobAsync(Guid jobId);

    /// <summary>Give an accepted job back to the marketplace before collecting anything.</summary>
    Task ReleaseJobAsync(Guid jobId);

    /// <summary>D06 – customer's pickup code verified, items collected (order → PickedUp).</summary>
    Task ConfirmPickupAsync(Guid jobId, DriverPickupRequest request);

    /// <summary>D08 – items handed to the provider (pickup job completed).</summary>
    Task ConfirmHandoverAsync(Guid jobId, string? notes, IReadOnlyList<string> photoPaths);

    /// <summary>D09 – package collected from the provider (order → OutForDelivery).</summary>
    Task StartDeliveryAsync(Guid jobId, int packageCount, IReadOnlyList<string> photoPaths);

    /// <summary>D11 – customer's delivery OTP verified (order → Delivered → Completed).</summary>
    Task ConfirmDeliveryAsync(Guid jobId, string otp, IReadOnlyList<string> photoPaths);

    Task<DriverEarnings> GetEarningsAsync(EarningsPeriod period);
    Task<DriverProfile> GetProfileAsync();
    Task UpdateProfileAsync(string name, string? email, string? phone, string licenseNumber);
}
