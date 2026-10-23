using DryCleaning.Configuration;
using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>Marketplace driver: go online, take jobs, collect, hand over and deliver.</summary>
public sealed class ApiDriverService(ApiClient api, AppSettings settings) : IDriverService
{
    private static ApiJsonContext Json => ApiJsonContext.Default;

    public async Task<DriverDashboard> GetDashboardAsync()
    {
        var d = await api.GetAsync("api/driver/dashboard", Json.DriverDashboardDto);
        return new DriverDashboard
        {
            DriverName = d.DriverName,
            IsOnline = d.IsAvailable,
            AvailableCount = d.AvailableJobs,
            ActiveJob = d.ActiveJob is null ? null : ApiMapping.ToJobDetail(d.ActiveJob),
            CompletedToday = d.CompletedToday,
            EarningsToday = d.EarningsToday,
            UnreadNotifications = d.UnreadNotifications,
            TodaysJobs = d.TodaysJobs.Select(ApiMapping.ToJobSummary).ToList()
        };
    }

    public Task<bool> SetOnlineAsync(bool isOnline) =>
        api.SendAsync(HttpMethod.Put, "api/driver/availability", new SetActiveRequest { IsActive = isOnline }, Json.SetActiveRequest, Json.Boolean);

    public async Task<IReadOnlyList<DriverJobSummary>> GetJobsAsync(DriverJobFilter filter)
    {
        switch (filter)
        {
            case DriverJobFilter.Available:
                var available = await api.GetAsync("api/driver/jobs/available?pageSize=100", Json.PagedResultDriverJobDto);
                return available.Items.Select(ApiMapping.ToJobSummary).ToList();

            case DriverJobFilter.Mine:
                var mine = await api.GetAsync("api/driver/jobs?pageSize=100", Json.PagedResultDriverJobDto);
                return mine.Items
                    .Where(j => j.Status is DriverJobStatus.Assigned or DriverJobStatus.InProgress)
                    .OrderBy(j => j.AssignedAt)
                    .Select(ApiMapping.ToJobSummary)
                    .ToList();

            default:
                var history = await api.GetAsync("api/driver/jobs/history?pageSize=100", Json.PagedResultDriverJobDto);
                return history.Items.Select(ApiMapping.ToJobSummary).ToList();
        }
    }

    public async Task<IReadOnlyDictionary<DriverJobFilter, int>> GetJobCountsAsync()
    {
        var c = await api.GetAsync("api/driver/jobs/counts", Json.DriverJobCountsDto);
        return new Dictionary<DriverJobFilter, int>
        {
            [DriverJobFilter.Available] = c.Available,
            [DriverJobFilter.Mine] = c.Mine,
            [DriverJobFilter.Completed] = c.Completed
        };
    }

    public async Task<DriverJobDetail> GetJobAsync(Guid jobId) =>
        ApiMapping.ToJobDetail(await api.GetAsync($"api/driver/jobs/{jobId}", Json.DriverJobDetailDto));

    public async Task<Guid?> FindJobForOrderAsync(Guid orderId)
    {
        try
        {
            var detail = await api.GetAsync($"api/driver/jobs/by-order/{orderId}", Json.DriverJobDetailDto);
            return detail.Job.Id;
        }
        catch (ApiException ex) when (ex.IsNotFound)
        {
            return null;
        }
    }

    public Task AcceptJobAsync(Guid jobId) => api.PostAsync($"api/driver/jobs/{jobId}/accept");

    public Task ReleaseJobAsync(Guid jobId) => api.PostAsync($"api/driver/jobs/{jobId}/release");

    public async Task ConfirmPickupAsync(Guid jobId, Models.DriverPickupRequest request)
    {
        var orderId = await OrderIdAsync(jobId);
        await api.UploadPhotosAsync(orderId, PhotoType.Pickup, request.PhotoPaths);
        await api.SendAsync(HttpMethod.Post, $"api/driver/jobs/{jobId}/pickup",
            new Contracts.DriverPickupRequest
            {
                Code = request.PickupCode.Trim(),
                Items = request.Items.Select(i => new DriverItemCheckRequest
                {
                    OrderItemId = i.OrderItemId,
                    CountedQuantity = i.CountedQuantity,
                    Condition = i.Condition,
                    Note = string.IsNullOrWhiteSpace(i.Note) ? null : i.Note.Trim()
                }).ToList(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            },
            Json.DriverPickupRequest);
    }

    public async Task ConfirmHandoverAsync(Guid jobId, string? notes, IReadOnlyList<string> photoPaths)
    {
        var orderId = await OrderIdAsync(jobId);
        await api.UploadPhotosAsync(orderId, PhotoType.Pickup, photoPaths);
        await api.SendAsync(HttpMethod.Post, $"api/driver/jobs/{jobId}/handover",
            new VerifyCodeRequest { Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim() }, Json.VerifyCodeRequest);
    }

    public async Task StartDeliveryAsync(Guid jobId, int packageCount, IReadOnlyList<string> photoPaths)
    {
        var orderId = await OrderIdAsync(jobId);
        await api.UploadPhotosAsync(orderId, PhotoType.Completion, photoPaths);
        await api.SendAsync(HttpMethod.Post, $"api/driver/jobs/{jobId}/start-delivery",
            new StartDeliveryRequest { PackageCount = packageCount }, Json.StartDeliveryRequest);
    }

    public async Task ConfirmDeliveryAsync(Guid jobId, string otp, IReadOnlyList<string> photoPaths)
    {
        var orderId = await OrderIdAsync(jobId);
        await api.UploadPhotosAsync(orderId, PhotoType.Completion, photoPaths);
        await api.SendAsync(HttpMethod.Post, $"api/driver/jobs/{jobId}/deliver",
            new VerifyCodeRequest { Code = otp.Trim() }, Json.VerifyCodeRequest);
    }

    public async Task<DriverEarnings> GetEarningsAsync(EarningsPeriod period)
    {
        var e = await api.GetAsync($"api/driver/earnings?period={period}", Json.DriverEarningsDto);
        return new DriverEarnings
        {
            Period = e.Period,
            PeriodText = $"{e.From:d MMM} – {e.To:d MMM yyyy}",
            Total = e.Total,
            JobCount = e.JobCount,
            PickupCount = e.PickupCount,
            DeliveryCount = e.DeliveryCount,
            RatePerJob = e.RatePerJob,
            Bars = e.Bars.Select(b => new EarningsBar { Label = b.Label, Amount = b.Amount }).ToList(),
            Jobs = e.Jobs.Select(ApiMapping.ToJobSummary).ToList()
        };
    }

    public async Task<DriverProfile> GetProfileAsync()
    {
        var profileTask = api.GetAsync("api/driver/profile", Json.DriverProfileDto);
        var countsTask = api.GetAsync("api/driver/jobs/counts", Json.DriverJobCountsDto);
        await Task.WhenAll(profileTask, countsTask);

        var p = profileTask.Result;
        var completed = countsTask.Result.Completed;
        return new DriverProfile
        {
            User = ApiMapping.ToUser(p.User),
            Driver = new Driver
            {
                Id = p.Id,
                UserId = p.User.Id,
                AddressId = p.Address?.Id ?? Guid.Empty,
                LicenseNumber = p.LicenseNumber,
                IsAvailable = p.IsAvailable,
                IsActive = p.IsActive
            },
            BaseAddress = p.Address is null ? null : ApiMapping.ToAddress(p.Address),
            CompletedJobs = completed,
            TotalEarnings = p.TotalEarnings
        };
    }

    public Task UpdateProfileAsync(string name, string? email, string? phone, string licenseNumber) =>
        api.SendAsync(HttpMethod.Put, "api/driver/profile",
            new UpdateDriverProfileRequest
            {
                LicenseNumber = licenseNumber.Trim(),
                Name = name.Trim(),
                Email = email?.Trim() ?? string.Empty, // empty clears the email
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
            },
            Json.UpdateDriverProfileRequest);

    /// <summary>Photos are stored per order, so look up which order a job belongs to.</summary>
    private async Task<Guid> OrderIdAsync(Guid jobId) =>
        (await api.GetAsync($"api/driver/jobs/{jobId}", Json.DriverJobDetailDto)).Job.OrderId;
}
