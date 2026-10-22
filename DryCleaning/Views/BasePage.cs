using DryCleaning.ViewModels;

namespace DryCleaning.Views;

/// <summary>Base page that forwards OnAppearing to the ViewModel so it can (re)load data.</summary>
public class BasePage : ContentPage
{
    private readonly BackButtonBehavior _backButton = new()
    {
        // iOS 26 draws the default back arrow dark on a glass button and ignores the white header colour,
        // so every page gets its own white arrow (Font Awesome chevron-left).
        IconOverride = new FontImageSource { FontFamily = "FA", Glyph = "\uf053", Size = 18, Color = Colors.White }
    };

    public BasePage() => Shell.SetBackButtonBehavior(this, _backButton);

    protected override void OnParentSet()
    {
        base.OnParentSet();
        // A page that is a ShellContent (a tab or a start page) is a root: nothing to go back to. iOS 26 draws a custom arrow
        // there even when it is hidden, so it is detached. Every pushed page keeps the arrow, attached before it appears.
        Shell.SetBackButtonBehavior(this, Parent is ShellContent ? null : _backButton);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel viewModel)
        {
            viewModel.AttachRealtime();
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as BaseViewModel)?.DetachRealtime();
    }
}
