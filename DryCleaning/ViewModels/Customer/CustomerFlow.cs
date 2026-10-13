using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

public static class CustomerFlow
{
    public static bool IsActive(OrderStatus status) =>
        status is not (OrderStatus.Completed or OrderStatus.Delivered or OrderStatus.Cancelled);
}
