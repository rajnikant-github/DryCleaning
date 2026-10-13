using DryCleaning.ViewModels.Auth;

namespace DryCleaning.Views.Auth;

public partial class OtpPage : BasePage
{
    public OtpPage(OtpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(400), () => OtpInput.FocusInput());
    }
}
