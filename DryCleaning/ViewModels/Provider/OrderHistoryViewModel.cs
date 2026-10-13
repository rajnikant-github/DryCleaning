using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P10 – Completed / Cancelled / Disputed orders.</summary>
public sealed class OrderHistoryViewModel(ViewModelServices services, IProviderOrderService orderService)
    : OrderListViewModelBase(services, orderService,
        (ProviderOrderFilter.Completed, "Completed"),
        (ProviderOrderFilter.Cancelled, "Cancelled"),
        (ProviderOrderFilter.Disputed, "Disputed"));
