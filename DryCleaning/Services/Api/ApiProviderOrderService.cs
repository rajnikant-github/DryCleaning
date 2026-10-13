using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>Provider order workflow: accept → receive → inspect → process → quality check → ready → driver.</summary>
public sealed class ApiProviderOrderService(ApiClient api, ISessionService session) : IProviderOrderService
{
    private static ApiJsonContext Json => ApiJsonContext.Default;

    public async Task<ProviderDashboard> GetDashboardAsync()
    {
        var dashboardTask = api.GetAsync("api/provider/dashboard", Json.ProviderDashboardDto);
        var incomingTask = api.GetAsync("api/provider/orders?filter=Incoming&pageSize=50", Json.PagedResultOrderSummaryDto);
        await Task.WhenAll(dashboardTask, incomingTask);

        var dashboard = dashboardTask.Result;
        var arriving = incomingTask.Result.Items
            .Where(o => o.Status == OrderStatus.PickedUp)
            .OrderByDescending(o => o.UpdatedAt)
            .FirstOrDefault();

        return new ProviderDashboard
        {
            BusinessName = dashboard.BusinessName,
            OwnerName = session.CurrentUser?.Name ?? dashboard.BusinessName,
            NewCount = dashboard.NewCount,
            InProcessCount = dashboard.InProcessCount,
            ReadyCount = dashboard.ReadyCount,
            CompletedCount = dashboard.CompletedCount,
            UnreadNotifications = dashboard.UnreadNotifications,
            IncomingDriverOrder = arriving is null ? null : ApiMapping.ToSummary(arriving),
            TodaysOrders = dashboard.TodaysOrders.Select(ApiMapping.ToSummary).ToList(),
            RecentActivity = dashboard.RecentActivity.Select(ApiMapping.ToActivity).ToList()
        };
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(ProviderOrderFilter filter)
    {
        var page = await api.GetAsync($"api/provider/orders?filter={filter}&pageSize=100", Json.PagedResultOrderSummaryDto);
        return page.Items.Select(ApiMapping.ToSummary).ToList();
    }

    public async Task<IReadOnlyDictionary<ProviderOrderFilter, int>> GetOrderCountsAsync()
    {
        var c = await api.GetAsync("api/provider/orders/counts", Json.ProviderOrderCountsDto);
        return new Dictionary<ProviderOrderFilter, int>
        {
            [ProviderOrderFilter.New] = c.New,
            [ProviderOrderFilter.Incoming] = c.Incoming,
            [ProviderOrderFilter.InProcess] = c.InProcess,
            [ProviderOrderFilter.Ready] = c.Ready,
            [ProviderOrderFilter.Completed] = c.Completed,
            [ProviderOrderFilter.Cancelled] = c.Cancelled,
            [ProviderOrderFilter.Disputed] = c.Disputed
        };
    }

    public async Task<OrderDetail> GetOrderDetailAsync(Guid orderId) =>
        ApiMapping.ToDetail(await api.GetAsync($"api/provider/orders/{orderId}", Json.OrderDetailDto), api.AbsoluteUrl);

    public Task AcceptOrderAsync(Guid orderId) => api.PostAsync($"api/provider/orders/{orderId}/accept");

    public Task RejectOrderAsync(Guid orderId, string reason) =>
        api.SendAsync(HttpMethod.Post, $"api/provider/orders/{orderId}/reject", new ReasonRequest { Reason = reason }, Json.ReasonRequest);

    public async Task ConfirmReceivedAsync(Guid orderId, Models.ReceiveOrderRequest request)
    {
        await api.UploadPhotosAsync(orderId, PhotoType.Inspection, request.PhotoPaths);
        await api.SendAsync(HttpMethod.Post, $"api/provider/orders/{orderId}/receive",
            new Contracts.ReceiveOrderRequest
            {
                ActualItemCount = request.ActualItemCount,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            },
            Json.ReceiveOrderRequest);
    }

    public async Task CompleteInspectionAsync(Guid orderId, IReadOnlyList<InspectionResult> results)
    {
        foreach (var result in results)
            await api.UploadPhotosAsync(orderId, result.Condition == ItemCondition.Damaged ? PhotoType.Damage : PhotoType.Inspection, result.PhotoPaths);

        await api.SendAsync(HttpMethod.Post, $"api/provider/orders/{orderId}/inspection",
            new InspectionRequest
            {
                Items = results.Select(r => new InspectionItemRequest
                {
                    OrderItemId = r.OrderItemId,
                    ReceivedQuantity = r.ReceivedQuantity,
                    Condition = r.Condition,
                    Note = string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim()
                }).ToList()
            },
            Json.InspectionRequest);
    }

    public Task MarkQualityCheckAsync(Guid orderId) => api.PostAsync($"api/provider/orders/{orderId}/quality-check");

    public Task SendBackToProcessingAsync(Guid orderId, string reason) =>
        api.SendAsync(HttpMethod.Post, $"api/provider/orders/{orderId}/reprocess", new ReasonRequest { Reason = reason }, Json.ReasonRequest);

    public async Task MarkReadyForDeliveryAsync(Guid orderId, IReadOnlyList<string> photoPaths)
    {
        await api.UploadPhotosAsync(orderId, PhotoType.Completion, photoPaths);
        await api.PostAsync($"api/provider/orders/{orderId}/ready");
    }

    public Task RequestDeliveryDriverAsync(Guid orderId) => api.PostAsync($"api/provider/orders/{orderId}/request-driver");

    public Task ReportIssueAsync(Guid orderId, string reason, string description) =>
        api.SendAsync(HttpMethod.Post, $"api/provider/orders/{orderId}/issues",
            new IssueRequest { Reason = reason, Description = description }, Json.IssueRequest);

    public Task RaiseDisputeAsync(Guid orderId, string reason, string description) =>
        api.SendAsync(HttpMethod.Post, $"api/provider/orders/{orderId}/disputes",
            new IssueRequest { Reason = reason, Description = description }, Json.IssueRequest);

    public Task SimulateDriverPickupAsync(Guid orderId) =>
        throw new InvalidOperationException("Simulating a driver is only available with demo data. A marketplace driver collects the items in the Driver app.");
}
