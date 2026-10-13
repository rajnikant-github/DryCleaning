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
    private readonly ICartService _cart;
    private readonly List<DateTime> _dates = [];
    private string _mode = "checkout";

    public PickupTimeViewModel(ViewModelServices services, ICartService cart) : base(services)
    {
        _cart = cart;
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

    protected override Task<bool> LoadDataAsync()
    {
        if (Dates.Count == 0)
        {
            for (var i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(i);
                _dates.Add(date);
                var title = i switch { 0 => "Today", 1 => "Tomorrow", _ => date.ToString("ddd d MMM") };
                Dates.Add(new SegmentItem(date.ToString("yyyy-MM-dd"), title, OnDateSelected));
            }

            var initial = _cart.PickupDate?.Date ?? DateTime.Today;
            if (initial < DateTime.Today || !AvailableSlots(initial).Any())
                initial = DateTime.Today.AddDays(1);
            SelectDate(initial, _cart.PickupSlot);
        }

        return Task.FromResult(true);
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

    private static IEnumerable<string> AvailableSlots(DateTime date)
    {
        // Same-day pickups need at least an hour's notice before the window starts.
        var cutoff = DateTime.Now.AddHours(1);
        return PickupSlots.All
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
