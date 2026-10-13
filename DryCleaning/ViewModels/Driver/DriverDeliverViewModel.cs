using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D11 – Deliver with the customer's OTP → Delivered → Completed.</summary>
public sealed partial class DriverDeliverViewModel(ViewModelServices services, IDriverService drivers, IPhotoService photos)
    : DriverJobViewModelBase(services, drivers)
{
    public const int CodeLength = 4;

    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string code = string.Empty;

    [ObservableProperty] private bool hasCodeError;
    [ObservableProperty] private string codeError = string.Empty;

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d) => Title = "Deliver Order";

    partial void OnCodeChanged(string value) => HasCodeError = false;

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    private bool CanConfirm() => Code.Length == CodeLength;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await Drivers.ConfirmDeliveryAsync(JobId, Code, Photos.ToList());
            await ReplaceWithAsync(Routes.DriverJobCompleted);
        }
        catch (InvalidOperationException ex)
        {
            CodeError = ex.Message;
            HasCodeError = true;
            Code = string.Empty;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Couldn't confirm delivery", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CustomerUnavailableAsync()
    {
        var call = await Dialogs.ConfirmAsync("Customer not available",
            "Call the customer first. If you can't reach them after 10 minutes, contact support on +27 21 555 0100 – don't leave the order unattended.",
            "Call customer", "Close");
        if (call)
            await CallAsync($"Call {CustomerName}", DestinationPhone);
    }
}
