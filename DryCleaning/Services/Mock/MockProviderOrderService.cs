using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockProviderOrderService(MockDataStore store, ISessionService session) : IProviderOrderService
{
    private Guid ProviderId => session.ProviderId ?? store.DemoProvider.Id;
    private Guid UserId => session.CurrentUser?.Id ?? store.DemoProvider.UserId;

    private IEnumerable<Order> ProviderOrders => store.Orders.Where(o =>
        o.ServiceProviderId == ProviderId &&
        (o.Status != OrderStatus.Created || store.Payments.Any(p => p.OrderId == o.Id && p.Status == PaymentStatus.Success)));

    // ── Queries ──────────────────────────────────────────────────────────────

    public async Task<ProviderDashboard> GetDashboardAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var orders = ProviderOrders.ToList();
            var provider = store.Providers.First(p => p.Id == ProviderId);
            var owner = store.UserById(provider.UserId);

            var incoming = orders
                .Where(o => o.Status == OrderStatus.PickedUp)
                .OrderByDescending(o => o.UpdatedAt)
                .FirstOrDefault();

            var activity = store.OrderStatusHistory
                .Where(h => orders.Any(o => o.Id == h.OrderId))
                .OrderByDescending(h => h.CreatedAt)
                .Take(6)
                .Select(h =>
                {
                    var order = orders.First(o => o.Id == h.OrderId);
                    return new ActivityEntry
                    {
                        OrderId = order.Id,
                        OrderNumber = order.OrderNumber,
                        Status = h.Status,
                        Text = $"{order.OrderNumber} · {h.Status.TimelineTitle()}",
                        At = h.CreatedAt
                    };
                })
                .ToList();

            return new ProviderDashboard
            {
                BusinessName = provider.BusinessName,
                OwnerName = owner.Name,
                NewCount = orders.Count(o => ProviderOrderFilter.New.Matches(o.Status)),
                InProcessCount = orders.Count(o => ProviderOrderFilter.InProcess.Matches(o.Status)),
                ReadyCount = orders.Count(o => ProviderOrderFilter.Ready.Matches(o.Status)),
                CompletedCount = orders.Count(o => ProviderOrderFilter.Completed.Matches(o.Status)),
                UnreadNotifications = store.Notifications.Count(n => n.UserId == owner.Id && !n.IsRead),
                IncomingDriverOrder = incoming is null ? null : ToSummary(incoming),
                TodaysOrders = orders
                    .Where(o => o.CreatedAt.Date == DateTime.Today || o.UpdatedAt.Date == DateTime.Today)
                    .Where(o => o.Status is not (OrderStatus.Cancelled or OrderStatus.Completed))
                    .OrderByDescending(o => o.UpdatedAt)
                    .Take(5)
                    .Select(ToSummary)
                    .ToList(),
                RecentActivity = activity
            };
        }
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(ProviderOrderFilter filter)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            return ProviderOrders
                .Where(o => filter.Matches(o.Status))
                .OrderByDescending(o => o.UpdatedAt)
                .Select(ToSummary)
                .ToList();
        }
    }

    public Task<IReadOnlyDictionary<ProviderOrderFilter, int>> GetOrderCountsAsync()
    {
        lock (store.Gate)
        {
            var orders = ProviderOrders.ToList();
            IReadOnlyDictionary<ProviderOrderFilter, int> counts = Enum.GetValues<ProviderOrderFilter>()
                .ToDictionary(f => f, f => orders.Count(o => f.Matches(o.Status)));
            return Task.FromResult(counts);
        }
    }

    public async Task<OrderDetail> GetOrderDetailAsync(Guid orderId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return MockMapping.ToDetail(store, store.OrderById(orderId), includeCustomerCodes: false);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public async Task AcceptOrderAsync(Guid orderId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            ChangeStatus(order, OrderStatus.Confirmed, "Accepted by provider");

            // Publish a pickup job (customer → provider) to the shared driver marketplace.
            store.PublishDriverJob(order, DriverJobType.Pickup);
            var provider = store.ProviderById(order.ServiceProviderId);
            store.Notify(store.CustomerById(order.CustomerId).UserId, order, "Order confirmed",
                $"{provider.BusinessName} accepted {order.OrderNumber}. We're finding a driver for pickup.");
            store.NotifyAvailableDrivers(order, "New pickup job", $"Collect {order.OrderNumber} for {provider.BusinessName}.");
        }
    }

    public async Task RejectOrderAsync(Guid orderId, string reason)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            ChangeStatus(order, OrderStatus.Cancelled, $"Rejected: {reason}");
        }
    }

    public async Task SimulateDriverPickupAsync(Guid orderId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            var job = store.DriverJobs.LastOrDefault(j => j.OrderId == order.Id && j.JobType == DriverJobType.Pickup && j.Status != DriverJobStatus.Cancelled)
                      ?? store.PublishDriverJob(order, DriverJobType.Pickup);

            if (order.Status == OrderStatus.Confirmed)
            {
                // Demo: a marketplace driver (never the demo driver account) accepts the pending job.
                var driver = PickAvailableDriver();
                job.DriverId = driver.Id;
                job.Status = DriverJobStatus.Assigned;
                job.AssignedAt = DateTime.Now;
                ChangeStatus(order, OrderStatus.PickupAssigned, $"Pickup assigned to {store.UserById(driver.UserId).Name}");
            }

            job.Status = DriverJobStatus.InProgress;
            var pickupCode = store.OrderVerifications.FirstOrDefault(v => v.OrderId == order.Id && v.Type == VerificationType.QR && v.VerifiedAt == null);
            if (pickupCode is not null)
            {
                pickupCode.VerifiedAt = DateTime.Now;
                pickupCode.VerifiedByUserId = job.DriverId is { } driverId ? store.DriverById(driverId).UserId : null;
            }
            ChangeStatus(order, OrderStatus.PickedUp, "Items collected from customer");
        }
    }

    public async Task ConfirmReceivedAsync(Guid orderId, ReceiveOrderRequest request)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            OrderWorkflow.EnsureCanTransition(order.Status, OrderStatus.ReceivedByProvider);

            var job = store.DriverJobs.LastOrDefault(j => j.OrderId == order.Id && j.JobType == DriverJobType.Pickup);
            if (job is not null)
            {
                job.Status = DriverJobStatus.Completed;
                job.CompletedAt = DateTime.Now;
                job.UpdatedAt = DateTime.Now;
            }

            store.OrderVerifications.Add(new OrderVerification
            {
                OrderId = order.Id,
                Type = VerificationType.Item,
                VerifiedAt = DateTime.Now,
                VerifiedByUserId = UserId
            });
            AddPhotos(order.Id, PhotoType.Inspection, request.PhotoPaths);

            var expected = store.OrderItems.Where(i => i.OrderId == order.Id).Sum(i => i.Quantity);
            var note = $"Received {request.ActualItemCount} of {expected} items.";
            if (!string.IsNullOrWhiteSpace(request.Notes))
                note += $" {request.Notes.Trim()}";

            ChangeStatus(order, OrderStatus.ReceivedByProvider, note);
        }
    }

    public async Task CompleteInspectionAsync(Guid orderId, IReadOnlyList<InspectionResult> results)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            OrderWorkflow.EnsureCanTransition(order.Status, OrderStatus.Processing);

            foreach (var result in results)
            {
                var type = result.Condition == ItemCondition.Damaged ? PhotoType.Damage : PhotoType.Inspection;
                AddPhotos(order.Id, type, result.PhotoPaths);
            }

            var issues = results
                .Where(r => r.Condition != ItemCondition.Good || r.ReceivedQuantity != r.ExpectedQuantity)
                .Select(r =>
                {
                    var parts = new List<string> { $"{r.ItemLabel}: {r.Condition}" };
                    if (r.ReceivedQuantity != r.ExpectedQuantity)
                        parts.Add($"{r.ReceivedQuantity}/{r.ExpectedQuantity} pcs");
                    if (!string.IsNullOrWhiteSpace(r.Note))
                        parts.Add(r.Note.Trim());
                    return string.Join(", ", parts);
                })
                .ToList();

            var notes = issues.Count == 0
                ? "Inspection complete · all items in good condition"
                : "Inspection complete · " + string.Join("; ", issues);

            ChangeStatus(order, OrderStatus.Processing, notes);
        }
    }

    public async Task MarkQualityCheckAsync(Guid orderId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            ChangeStatus(store.OrderById(orderId), OrderStatus.QualityCheck, "Cleaning finished · sent for quality check");
    }

    public async Task SendBackToProcessingAsync(Guid orderId, string reason)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            ChangeStatus(store.OrderById(orderId), OrderStatus.Processing, $"Quality check failed: {reason}");
    }

    public async Task MarkReadyForDeliveryAsync(Guid orderId, IReadOnlyList<string> photoPaths)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            OrderWorkflow.EnsureCanTransition(order.Status, OrderStatus.ReadyForDelivery);
            AddPhotos(order.Id, PhotoType.Completion, photoPaths);
            ChangeStatus(order, OrderStatus.ReadyForDelivery, "Quality check passed · packed for delivery");
        }
    }

    public async Task RequestDeliveryDriverAsync(Guid orderId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            if (order.Status != OrderStatus.ReadyForDelivery)
                throw new InvalidOperationException("Only orders that are ready for delivery can request a driver.");
            if (store.DriverJobs.Any(j => j.OrderId == order.Id && j.JobType == DriverJobType.Delivery &&
                                          j.Status is DriverJobStatus.Pending or DriverJobStatus.Assigned or DriverJobStatus.InProgress))
                throw new InvalidOperationException("A delivery driver has already been requested for this order.");

            // Publish a delivery job (provider → customer) to the shared driver marketplace.
            store.PublishDriverJob(order, DriverJobType.Delivery);
            store.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                ChangedByUserId = UserId,
                Notes = "Delivery driver requested · waiting for a driver to accept"
            });
            store.NotifyAvailableDrivers(order, "New delivery job",
                $"Deliver {order.OrderNumber} from {store.ProviderById(order.ServiceProviderId).BusinessName}.");
        }
    }

    public async Task ReportIssueAsync(Guid orderId, string reason, string description)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            store.Disputes.Add(new Dispute
            {
                OrderId = order.Id,
                RaisedByUserId = UserId,
                Reason = reason,
                Description = description,
                Status = DisputeStatus.Open
            });
            store.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                ChangedByUserId = UserId,
                Notes = $"Issue reported – {reason}: {description}"
            });
        }
    }

    public async Task RaiseDisputeAsync(Guid orderId, string reason, string description)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var order = store.OrderById(orderId);
            OrderWorkflow.EnsureCanTransition(order.Status, OrderStatus.Disputed);
            store.Disputes.Add(new Dispute
            {
                OrderId = order.Id,
                RaisedByUserId = UserId,
                Reason = reason,
                Description = description,
                Status = DisputeStatus.Open
            });
            ChangeStatus(order, OrderStatus.Disputed, $"{reason}: {description}");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ChangeStatus(Order order, OrderStatus to, string? notes)
    {
        OrderWorkflow.EnsureCanTransition(order.Status, to);
        order.Status = to;
        order.UpdatedAt = DateTime.Now;
        store.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = to,
            ChangedByUserId = UserId,
            Notes = notes
        });
    }

    private void AddPhotos(Guid orderId, PhotoType type, IEnumerable<string> paths)
    {
        foreach (var path in paths)
            store.OrderPhotos.Add(new OrderPhoto { OrderId = orderId, Type = type, Url = path, UploadedBy = UploadedBy.Provider });
    }

    private Driver PickAvailableDriver()
    {
        var available = store.Drivers.Where(d => d.IsActive && d.IsAvailable && d.Id != store.DemoDriver.Id).ToList();
        return available.Count == 0 ? store.Drivers.First(d => d.Id != store.DemoDriver.Id) : available[Random.Shared.Next(available.Count)];
    }

    private OrderSummary ToSummary(Order order) => MockMapping.ToSummary(store, order);

    private OrderItemDetail ToItemDetail(OrderItem item) => MockMapping.ToItemDetail(store, item);
}
