using DryCleaning.ViewModels;
using DryCleaning.Services;

namespace DryCleaning.Views;

public partial class IncomingOrderAlertPage : BasePage
{
    private readonly IIncomingAlertFeedbackService _feedback;

    public IncomingOrderAlertPage(IncomingOrderAlertViewModel viewModel, IIncomingAlertFeedbackService feedback)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _feedback = feedback;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _feedback.PlayAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IncomingOrderAlert] Feedback could not start: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        _feedback.Stop();
        base.OnDisappearing();
    }
}
