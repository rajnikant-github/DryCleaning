using DryCleaning.ViewModels;

namespace DryCleaning.Views;

/// <summary>Base page that forwards OnAppearing to the ViewModel so it can (re)load data.</summary>
public class BasePage : ContentPage
{
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel viewModel)
        {
            try
            {
                await viewModel.OnAppearingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] OnAppearing failed: {ex}");
            }
        }
    }
}
