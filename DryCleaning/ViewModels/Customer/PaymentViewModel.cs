using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C14 – Payment (Paystack).</summary>
public sealed partial class PaymentViewModel(
    ViewModelServices services, ICustomerOrderService orders, IPaymentService payments, ICartService cart)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _orderId;

    [ObservableProperty] private string orderNumber = string.Empty;
    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string payButtonText = "Pay";
    [ObservableProperty] private bool isProcessing;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string paymentError = string.Empty;

    public string CurrencyCode => Services.Settings.Currency.Code;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.OrderIdKey, out var id))
            _orderId = id;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var detail = await orders.GetOrderAsync(_orderId);
        if (detail.IsPaid)
        {
            await Navigation.GoToAsync($"../{Routes.OrderConfirmed}", Param(Routes.OrderIdKey, _orderId));
            return true;
        }

        OrderNumber = detail.Order.OrderNumber;
        ProviderName = detail.ProviderName;
        Total = detail.Order.TotalAmount;
        PayButtonText = $"Pay {CurrencyFormatter.Current.Format(Total)}";
        return true;
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (IsProcessing)
            return;

        try
        {
            IsProcessing = true;
            HasError = false;
            var result = await payments.PayAsync(_orderId);
            if (!result.Success)
            {
                PaymentError = result.Message ?? "Payment was not completed. Please try again.";
                HasError = true;
                return;
            }

            cart.Clear();
            // Reset the stack to Home, then show the confirmation.
            await Navigation.GoToAsync($"{Routes.CustomerHome}/{Routes.OrderConfirmed}", Param(Routes.OrderIdKey, _orderId));
        }
        catch (Exception ex)
        {
            PaymentError = ex.Message;
            HasError = true;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
