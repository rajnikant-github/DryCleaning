using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C12 – Pickup date and time slot.</summary>
public sealed partial class PickupTimeViewModel : BaseViewModel, IQueryAttributable
{
    private const int DaysAhead = 14;
    private const int MaxDates = 7;

    private readonly ICartService _cart;
    private readonly ICustomerCatalogService _catalog;
    private readonly List<DateTime> _dates = [];
    private IReadOnlyList<WorkingHour> _hours = WorkingHour.DefaultWeek();
    private string _mode = "checkout";

    public PickupTimeViewModel(ViewModelServices services, ICartService cart, ICustomerCatalogService catalog) : base(services)
    {
        _cart = cart;
        _catalog = catalog;
    }

    public ObservableCollection<SegmentItem> Dates { get; } = [];
    public ObservableCollection<SlotItem> Slots { get; } = [];

    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private bool hasNoSlots;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private SlotItem? selectedSlot;

    public string ContinueText => _mode == "edit" ? "Save" : "Continue";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.ModeKey, out var mode) && mode is string m)
            _mode = m;
        OnPropertyChanged(nameof(ContinueText));
    }

    protected override async Task<bool> LoadDataAsync()
    {
        _hours = _cart.ProviderId is { } providerId
            ? await _catalog.GetWorkingHoursAsync(providerId)
            : WorkingHour.DefaultWeek();

        Dates.Clear();
        _dates.Clear();
        // Only days the provider is open (and still has a bookable window) are offered, up to a week of them.
        for (var i = 0; i < DaysAhead && _dates.Count < MaxDates; i++)
        {
            var date = DateTime.Today.AddDays(i);
            if (!AvailableSlots(date).Any())
                continue;

            _dates.Add(date);
            var title = date == DateTime.Today ? "Today" : date == DateTime.Today.AddDays(1) ? "Tomorrow" : date.ToString("ddd d MMM");
            Dates.Add(new SegmentItem(date.ToString("yyyy-MM-dd"), title, OnDateSelected));
        }

        Slots.Clear();
        SelectedSlot = null;
        HasNoSlots = _dates.Count == 0;
        if (_dates.Count > 0)
        {
            var preferred = _cart.PickupDate?.Date;
            var initial = preferred is { } p && _dates.Contains(p) ? p : _dates[0];
            SelectDate(initial, _cart.PickupSlot);
        }

        return true;
    }

    private void OnDateSelected(SegmentItem item)
    {
        if (DateTime.TryParse(item.Key, out var date))
            SelectDate(date, null);
    }

    private void SelectDate(DateTime date, string? preselectSlot)
    {
        SelectedDate = date;
        foreach (var d in Dates)
            d.IsSelected = d.Key == date.ToString("yyyy-MM-dd");

        Slots.Clear();
        var available = AvailableSlots(date).ToHashSet();
        foreach (var slot in PickupSlots.All)
        {
            var label = PickupSlots.Label(slot);
            var item = new SlotItem(label, available.Contains(label), SelectSlot);
            Slots.Add(item);
        }

        SelectedSlot = null;
        var match = Slots.FirstOrDefault(s => s.IsAvailable && s.Label == preselectSlot);
        if (match is not null)
            SelectSlot(match);
        HasNoSlots = Slots.All(s => !s.IsAvailable);
    }

    private IEnumerable<string> AvailableSlots(DateTime date)
    {
        // Same-day pickups need at least an hour's notice before the window starts, and the window must be inside the provider's hours.
        var cutoff = DateTime.Now.AddHours(1);
        return PickupSlots.All
            .Where(s => PickupSlots.IsWithinHours(s, _hours, date))
            .Where(s => date.Date > DateTime.Today || date.Date.Add(s.Start) >= cutoff)
            .Select(PickupSlots.Label);
    }

    private void SelectSlot(SlotItem slot)
    {
        foreach (var s in Slots)
            s.IsSelected = ReferenceEquals(s, slot);
        SelectedSlot = slot;
    }

    private bool CanContinue() => SelectedSlot is not null;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private Task ContinueAsync()
    {
        _cart.PickupDate = SelectedDate.Date;
        _cart.PickupSlot = SelectedSlot!.Label;
        return _mode == "edit" ? Navigation.GoBackAsync() : Navigation.GoToAsync(Routes.OrderSummary);
    }
}
