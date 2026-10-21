using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

/// <summary>
/// Shared marketplace driver (no ServiceProviderId). Same rules as the API's DriverService:
/// Pickup job:   accept → verify pickup code at the customer → hand over to the provider.
/// Delivery job: accept → collect from the provider → deliver with the customer's OTP.
/// </summary>
public sealed class MockDriverService(MockDataStore store, ISessionService session, AppSettings settings) : IDriverService
{
    private Driver Me => session.DriverId is { } id ? store.DriverById(id) : store.DemoDriver;
    private decimal Rate => settings.Pricing.DriverEarningPerJob;

    private IEnumerable<DriverJob> MyJobs => store.DriverJobs.Where(j => j.DriverId == Me.Id);

    private IEnumerable<DriverJob> MyActiveJobs =>
        MyJobs.Where(j => j.Status is DriverJobStatus.Assigned or DriverJobStatus.InProgress);

    private IEnumerable<DriverJob> AvailableJobs => store.DriverJobs.Where(j =>
        j.Status == DriverJobStatus.Pending &&
        store.OrderById(j.OrderId).Status is not (OrderStatus.Cancelled or OrderStatus.Disputed));

    // ── Queries ──────────────────────────────────────────────────────────────

    public async Task<DriverDashboard> GetDashboardAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var me = Me;
            var today = DateTime.Today;
            var active = MyActiveJobs.OrderBy(j => j.AssignedAt).FirstOrDefault();
            var completedToday = MyJobs.Count(j => j.Status == DriverJobStatus.Completed && j.CompletedAt?.Date == today);

            return new DriverDashboard
            {
                DriverName = store.UserById(me.UserId).Name,
                IsOnline = me.IsAvailable,
                AvailableCount = me.IsAvailable ? AvailableJobs.Count() : 0,
                ActiveJob = active is null ? null : ToDetail(active),
                CompletedToday = completedToday,
                EarningsToday = completedToday * Rate,
                UnreadNotifications = store.Notifications.Count(n => n.UserId == me.UserId && !n.IsRead),
                TodaysJobs = MyJobs
                    .Where(j => j.AssignedAt?.Date == today || j.CompletedAt?.Date == today)
                    .OrderByDescending(j => j.CompletedAt ?? j.AssignedAt)
                    .Select(ToSummary)
                    .ToList()
            };
        }
    }

    public async Task<bool> SetOnlineAsync(bool isOnline)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var me = Me;
            if (!me.IsActive)
                throw new InvalidOperationException("Your driver account is not active. Please contact E Dry Cleaners support.");
            me.IsAvailable = isOnline;
            me.UpdatedAt = DateTime.Now;
            return me.IsAvailable;
        }
    }

    public async Task<IReadOnlyList<DriverJobSummary>> GetJobsAsync(DriverJobFilter filter)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            IEnumerable<DriverJob> jobs = filter switch
            {
                DriverJobFilter.Available => Me.IsAvailable ? AvailableJobs.OrderBy(j => j.CreatedAt) : Enumerable.Empty<DriverJob>(),
                DriverJobFilter.Mine => MyActiveJobs.OrderBy(j => j.AssignedAt),
                _ => MyJobs.Where(j => j.Status is DriverJobStatus.Completed or DriverJobStatus.Cancelled)
                           .OrderByDescending(j => j.CompletedAt ?? j.UpdatedAt)
            };
            return jobs.Select(ToSummary).ToList();
        }
    }

    public Task<IReadOnlyDictionary<DriverJobFilter, int>> GetJobCountsAsync()
    {
        lock (store.Gate)
        {
            IReadOnlyDictionary<DriverJobFilter, int> counts = new Dictionary<DriverJobFilter, int>
            {
                [DriverJobFilter.Available] = Me.IsAvailable ? AvailableJobs.Count() : 0,
                [DriverJobFilter.Mine] = MyActiveJobs.Count(),
                [DriverJobFilter.Completed] = MyJobs.Count(j => j.Status == DriverJobStatus.Completed)
            };
            return Task.FromResult(counts);
        }
    }

    public async Task<DriverJobDetail> GetJobAsync(Guid jobId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return ToDetail(JobById(jobId));
    }

    public Task<Guid?> FindJobForOrderAsync(Guid orderId)
    {
        lock (store.Gate)
        {
            var job = MyJobs.LastOrDefault(j => j.OrderId == orderId)
                      ?? store.DriverJobs.LastOrDefault(j => j.OrderId == orderId && j.Status == DriverJobStatus.Pending)
                      ?? store.DriverJobs.LastOrDefault(j => j.OrderId == orderId);
            return Task.FromResult(job?.Id);
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public async Task AcceptJobAsync(Guid jobId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var me = Me;
            if (!me.IsAvailable)
                throw new InvalidOperationException("Go online to accept jobs.");
            if (MyActiveJobs.Any())
                throw new InvalidOperationException("Finish your active job before accepting another.");

            var job = JobById(jobId);
            if (job.Status != DriverJobStatus.Pending)
                throw new InvalidOperationException("This job has already been taken by another driver.");

            var order = store.OrderById(job.OrderId);
            EnsureNotOnHold(order);

            job.DriverId = me.Id;
            job.Status = DriverJobStatus.Assigned;
            job.AssignedAt = DateTime.Now;
            job.UpdatedAt = DateTime.Now;

            var name = store.UserById(me.UserId).Name;
            var customerUserId = store.CustomerById(order.CustomerId).UserId;
            var provider = store.ProviderById(order.ServiceProviderId);
            if (job.JobType == DriverJobType.Pickup)
            {
                ChangeStatus(order, OrderStatus.PickupAssigned, $"Pickup assigned to {name}");
                store.Notify(customerUserId, order, "Driver assigned", $"{name} will collect {order.OrderNumber}. Have your pickup code ready.");
            }
            else
            {
                ChangeStatus(order, OrderStatus.DeliveryAssigned, $"Delivery assigned to {name}");
                store.Notify(customerUserId, order, "Delivery driver assigned", $"{name} will deliver {order.OrderNumber}.");
            }
            store.Notify(provider.UserId, order, "Driver assigned",
                $"{name} accepted the {job.JobType.ToString().ToLowerInvariant()} job for {order.OrderNumber}.");
        }
    }

    public async Task ReleaseJobAsync(Guid jobId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var job = MyJob(jobId);
            if (job.Status != DriverJobStatus.Assigned)
                throw new InvalidOperationException("Only jobs that haven't started can be released.");

            var order = store.OrderById(job.OrderId);
            job.DriverId = null;
            job.Status = DriverJobStatus.Pending;
            job.AssignedAt = null;
            job.UpdatedAt = DateTime.Now;
            ChangeStatus(order,
                job.JobType == DriverJobType.Pickup ? OrderStatus.Confirmed : OrderStatus.ReadyForDelivery,
                "Driver released the job · back on the marketplace");
        }
    }

    public async Task ConfirmPickupAsync(Guid jobId, DriverPickupRequest request)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var job = MyJob(jobId);
            if (job.JobType != DriverJobType.Pickup || job.Status != DriverJobStatus.Assigned)
                throw new InvalidOperationException("This job is not waiting for a customer pickup.");

            var order = store.OrderById(job.OrderId);
            EnsureNotOnHold(order);
            var code = store.OrderVerifications.FirstOrDefault(v => v.OrderId == order.Id && v.Type == VerificationType.QR && v.VerifiedAt == null);
            if (code is null || code.OtpCode != request.PickupCode.Trim())
                throw new InvalidOperationException("The pickup code doesn't match. Ask the customer to show the code in their app.");

            code.VerifiedAt = DateTime.Now;
            code.VerifiedByUserId = Me.UserId;
            job.Status = DriverJobStatus.InProgress;
            job.UpdatedAt = DateTime.Now;
            AddPhotos(order.Id, PhotoType.Pickup, request.PhotoPaths);

            var counted = request.Items.Sum(i => i.CountedQuantity);
            var expected = store.OrderItems.Where(i => i.OrderId == order.Id).Sum(i => i.Quantity);
            var notes = new List<string> { request.Items.Count == 0 ? "Items collected from customer" : $"Collected {counted} of {expected} items from customer" };
            notes.AddRange(request.Items
                .Where(i => i.Condition != ItemCondition.Good || i.CountedQuantity != i.ExpectedQuantity)
                .Select(i => $"{i.ItemLabel}: {i.Condition}" +
                             (i.CountedQuantity != i.ExpectedQuantity ? $" ({i.CountedQuantity}/{i.ExpectedQuantity} pcs)" : "") +
                             (string.IsNullOrWhiteSpace(i.Note) ? "" : $" – {i.Note!.Trim()}")));
            if (!string.IsNullOrWhiteSpace(request.Notes))
                notes.Add(request.Notes.Trim());
            ChangeStatus(order, OrderStatus.PickedUp, string.Join(". ", notes));

            var provider = store.ProviderById(order.ServiceProviderId);
            store.Notify(provider.UserId, order, "Driver on the way", $"{order.OrderNumber} has been collected and is on its way to you.");
            store.Notify(store.CustomerById(order.CustomerId).UserId, order, "Items picked up",
                $"Your items for {order.OrderNumber} are on the way to {provider.BusinessName}.");
        }
    }

    public async Task ConfirmHandoverAsync(Guid jobId, string? notes, IReadOnlyList<string> photoPaths)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var job = MyJob(jobId);
            if (job.JobType != DriverJobType.Pickup || job.Status != DriverJobStatus.InProgress)
                throw new InvalidOperationException("This job is not ready for handover.");

            var order = store.OrderById(job.OrderId);
            job.Status = DriverJobStatus.Completed;
            job.CompletedAt = DateTime.Now;
            job.UpdatedAt = DateTime.Now;
            AddPhotos(order.Id, PhotoType.Pickup, photoPaths);
            store.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                ChangedByUserId = Me.UserId,
                Notes = string.IsNullOrWhiteSpace(notes) ? "Handed over to provider" : $"Handed over to provider. {notes.Trim()}"
            });
            store.Notify(store.ProviderById(order.ServiceProviderId).UserId, order, "Items delivered",
                $"{order.OrderNumber} was handed over by the driver. Please confirm receipt.");
        }
    }

    public async Task StartDeliveryAsync(Guid jobId, int packageCount, IReadOnlyList<string> photoPaths)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var job = MyJob(jobId);
            if (job.JobType != DriverJobType.Delivery || job.Status != DriverJobStatus.Assigned)
                throw new InvalidOperationException("This job is not waiting for collection from the provider.");

            var order = store.OrderById(job.OrderId);
            EnsureNotOnHold(order);
            job.Status = DriverJobStatus.InProgress;
            job.UpdatedAt = DateTime.Now;
            AddPhotos(order.Id, PhotoType.Completion, photoPaths);
            ChangeStatus(order, OrderStatus.OutForDelivery,
                $"Collected {packageCount} package{(packageCount == 1 ? "" : "s")} from provider · out for delivery");
            store.Notify(store.CustomerById(order.CustomerId).UserId, order, "Out for delivery",
                $"{order.OrderNumber} is on its way. Have your delivery OTP ready.");
        }
    }

    public async Task ConfirmDeliveryAsync(Guid jobId, string otp, IReadOnlyList<string> photoPaths)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var job = MyJob(jobId);
            if (job.JobType != DriverJobType.Delivery || job.Status != DriverJobStatus.InProgress)
                throw new InvalidOperationException("This job is not out for delivery.");

            var order = store.OrderById(job.OrderId);
            EnsureNotOnHold(order);
            var customer = store.CustomerById(order.CustomerId);
            var code = store.OrderVerifications.FirstOrDefault(v => v.OrderId == order.Id && v.Type == VerificationType.OTP && v.VerifiedAt == null);
            if (code is null || code.OtpCode != otp.Trim())
                throw new InvalidOperationException("Incorrect OTP. Ask the customer for the delivery code shown in their app.");

            code.VerifiedAt = DateTime.Now;
            code.VerifiedByUserId = customer.UserId;
            job.Status = DriverJobStatus.Completed;
            job.CompletedAt = DateTime.Now;
            job.UpdatedAt = DateTime.Now;
            AddPhotos(order.Id, PhotoType.Completion, photoPaths);

            ChangeStatus(order, OrderStatus.Delivered, "Delivered to customer · OTP verified");
            ChangeStatus(order, OrderStatus.Completed, "Order completed");

            store.Notify(customer.UserId, order, "Delivered", $"{order.OrderNumber} has been delivered. Thank you for using E Dry Cleaners!");
            store.Notify(store.ProviderById(order.ServiceProviderId).UserId, order, "Order completed",
                $"{order.OrderNumber} was delivered to the customer.");
        }
    }

    // ── Earnings & profile ───────────────────────────────────────────────────

    public async Task<DriverEarnings> GetEarningsAsync(EarningsPeriod period)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var today = DateTime.Today;
            var from = period == EarningsPeriod.Week ? today.AddDays(-6) : today.AddDays(-27);
            var jobs = MyJobs
                .Where(j => j.Status == DriverJobStatus.Completed && j.CompletedAt is { } at && at.Date >= from)
                .OrderByDescending(j => j.CompletedAt)
                .ToList();

            List<EarningsBar> bars;
            if (period == EarningsPeriod.Week)
            {
                bars = Enumerable.Range(0, 7)
                    .Select(i => from.AddDays(i))
                    .Select(day => new EarningsBar
                    {
                        Label = day == today ? "Today" : day.ToString("ddd"),
                        Amount = jobs.Count(j => j.CompletedAt!.Value.Date == day) * Rate
                    })
                    .ToList();
            }
            else
            {
                bars = Enumerable.Range(0, 4)
                    .Select(i => from.AddDays(i * 7))
                    .Select(start => new EarningsBar
                    {
                        Label = start.ToString("d MMM"),
                        Amount = jobs.Count(j => j.CompletedAt!.Value.Date >= start && j.CompletedAt.Value.Date < start.AddDays(7)) * Rate
                    })
                    .ToList();
            }

            return new DriverEarnings
            {
                Period = period,
                PeriodText = $"{from:d MMM} – {today:d MMM yyyy}",
                Total = jobs.Count * Rate,
                JobCount = jobs.Count,
                PickupCount = jobs.Count(j => j.JobType == DriverJobType.Pickup),
                DeliveryCount = jobs.Count(j => j.JobType == DriverJobType.Delivery),
                RatePerJob = Rate,
                Bars = bars,
                Jobs = jobs.Select(ToSummary).ToList()
            };
        }
    }

    public async Task<DriverProfile> GetProfileAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var me = Me;
            var completed = MyJobs.Count(j => j.Status == DriverJobStatus.Completed);
            return new DriverProfile
            {
                User = store.UserById(me.UserId),
                Driver = me,
                BaseAddress = store.Addresses.FirstOrDefault(a => a.Id == me.AddressId),
                CompletedJobs = completed,
                TotalEarnings = completed * Rate
            };
        }
    }

    public async Task UpdateProfileAsync(string name, string? email, string? phone, string licenseNumber)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var me = Me;
            var user = store.UserById(me.UserId);
            user.Name = name.Trim();
            user.Email = email?.Trim() ?? string.Empty;
            user.Phone = phone?.Trim() ?? string.Empty;
            user.UpdatedAt = DateTime.Now;
            me.LicenseNumber = licenseNumber.Trim();
            me.UpdatedAt = DateTime.Now;
        }
    }

    // ── Mapping ──────────────────────────────────────────────────────────────

    private DriverJobSummary ToSummary(DriverJob job)
    {
        var order = store.OrderById(job.OrderId);
        var customerUser = store.UserById(store.CustomerById(order.CustomerId).UserId);
        var provider = store.ProviderById(order.ServiceProviderId);
        var from = store.AddressById(job.PickupAddressId);
        var to = store.AddressById(job.DeliveryAddressId);
        var items = store.OrderItems.Where(i => i.OrderId == order.Id).ToList();
        var isPickup = job.JobType == DriverJobType.Pickup;

        return new DriverJobSummary
        {
            Id = job.Id,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            JobType = job.JobType,
            Status = job.Status,
            OrderStatus = order.Status,
            FromName = isPickup ? customerUser.Name : provider.BusinessName,
            FromAddress = from.FormattedAddress,
            FromArea = from.SubLocality ?? from.Locality ?? string.Empty,
            ToName = isPickup ? provider.BusinessName : customerUser.Name,
            ToAddress = to.FormattedAddress,
            ToArea = to.SubLocality ?? to.Locality ?? string.Empty,
            ItemCount = items.Sum(i => i.Quantity),
            ItemsText = string.Join(", ", items.Select(i => $"{i.Quantity} × {store.Products.First(p => p.Id == i.ProductId).Name.ToLowerInvariant()}")),
            Earnings = Rate,
            DistanceKm = Geo.DistanceKm(from, to),
            PickupDate = order.PickupDate,
            PickupSlot = order.PickupSlot,
            CreatedAt = job.CreatedAt,
            AssignedAt = job.AssignedAt,
            CompletedAt = job.CompletedAt,
            IsOnHold = order.Status == OrderStatus.Disputed
        };
    }

    private DriverJobDetail ToDetail(DriverJob job)
    {
        var order = store.OrderById(job.OrderId);
        var customerUser = store.UserById(store.CustomerById(order.CustomerId).UserId);
        var provider = store.ProviderById(order.ServiceProviderId);
        var isPickup = job.JobType == DriverJobType.Pickup;
        var isMine = job.DriverId == Me.Id;

        return new DriverJobDetail
        {
            Job = ToSummary(job),
            From = store.AddressById(job.PickupAddressId),
            To = store.AddressById(job.DeliveryAddressId),
            FromPhone = isPickup ? customerUser.Phone : provider.Phone,
            ToPhone = isPickup ? provider.Phone : customerUser.Phone,
            CustomerName = customerUser.Name,
            ProviderName = provider.BusinessName,
            Items = store.OrderItems.Where(i => i.OrderId == order.Id).Select(i => MockMapping.ToItemDetail(store, i)).ToList(),
            History = store.OrderStatusHistory.Where(h => h.OrderId == order.Id).OrderBy(h => h.CreatedAt).ToList(),
            NextStep = NextStep(job, order, isMine),
            IsMine = isMine,
            DemoCustomerCode = settings.Features.ShowDemoActions && isMine
                ? store.OrderVerifications.FirstOrDefault(v => v.OrderId == order.Id && v.VerifiedAt == null &&
                      v.Type == (isPickup ? VerificationType.QR : VerificationType.OTP))?.OtpCode
                : null
        };
    }

    private static DriverJobStep NextStep(DriverJob job, Order order, bool isMine)
    {
        if (job.Status == DriverJobStatus.Cancelled || order.Status == OrderStatus.Cancelled)
            return DriverJobStep.Unavailable;
        if (job.Status == DriverJobStatus.Pending)
            return order.Status == OrderStatus.Disputed ? DriverJobStep.Unavailable : DriverJobStep.Accept;
        if (!isMine)
            return DriverJobStep.Unavailable;

        return (job.JobType, job.Status) switch
        {
            (_, DriverJobStatus.Completed) => DriverJobStep.Done,
            (DriverJobType.Pickup, DriverJobStatus.Assigned) => DriverJobStep.CollectFromCustomer,
            (DriverJobType.Pickup, DriverJobStatus.InProgress) => DriverJobStep.DropAtProvider,
            (DriverJobType.Delivery, DriverJobStatus.Assigned) => DriverJobStep.CollectFromProvider,
            (DriverJobType.Delivery, DriverJobStatus.InProgress) => DriverJobStep.DeliverToCustomer,
            _ => DriverJobStep.Unavailable
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private DriverJob JobById(Guid jobId) =>
        store.DriverJobs.FirstOrDefault(j => j.Id == jobId) ?? throw new KeyNotFoundException("Job not found.");

    private DriverJob MyJob(Guid jobId)
    {
        var job = JobById(jobId);
        if (job.DriverId != Me.Id)
            throw new InvalidOperationException("This job isn't assigned to you.");
        return job;
    }

    private static void EnsureNotOnHold(Order order)
    {
        if (order.Status == OrderStatus.Disputed)
            throw new InvalidOperationException("This order is on hold while E Dry Cleaners support reviews an issue.");
        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("This order was cancelled.");
    }

    private void ChangeStatus(Order order, OrderStatus to, string? notes)
    {
        OrderWorkflow.EnsureCanTransition(order.Status, to);
        order.Status = to;
        order.UpdatedAt = DateTime.Now;
        store.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = to,
            ChangedByUserId = Me.UserId,
            Notes = notes
        });
    }

    private void AddPhotos(Guid orderId, PhotoType type, IEnumerable<string> paths)
    {
        foreach (var path in paths)
            store.OrderPhotos.Add(new OrderPhoto { OrderId = orderId, Type = type, Url = path, UploadedBy = UploadedBy.Driver });
    }
}
