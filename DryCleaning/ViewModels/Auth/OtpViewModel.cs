using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Mock;

namespace DryCleaning.ViewModels.Auth;

public sealed partial class OtpViewModel(ViewModelServices services, IAuthService auth, ISessionService session)
    : BaseViewModel(services), IQueryAttributable
{
    private const int ResendSeconds = 30;
    private CancellationTokenSource? _countdown;
    private string _identifier = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
    private string code = string.Empty;

    [ObservableProperty]
    private string destination = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanResend))]
    [NotifyPropertyChangedFor(nameof(ResendText))]
    private int secondsRemaining;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorText = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public bool CanResend => SecondsRemaining == 0;
    public string ResendText => CanResend ? "Resend OTP" : $"Resend OTP in {SecondsRemaining}s";
    public bool ShowDemoHint => !string.IsNullOrEmpty(auth.OtpHint);
    public string DemoHint => $"Demo code: {auth.OtpHint}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.IdentifierKey, out var value) && value is string identifier)
            _identifier = identifier;
    }

    public override async Task OnAppearingAsync()
    {
        State = ViewState.Content;
        if (string.IsNullOrEmpty(Destination))
            await SendCodeAsync();
    }

    private bool CanVerify() => Code.Length == 6;

    [RelayCommand(CanExecute = nameof(CanVerify))]
    private Task VerifyAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        var result = await auth.VerifyOtpAsync(_identifier, Code, session.SelectedRole);
        if (!result.Success)
        {
            ErrorText = result.Error ?? "Verification failed.";
            Code = string.Empty;
            return;
        }

        _countdown?.Cancel();
        await Navigation.GoToAsync(Routes.HomeFor(session.CurrentUser?.Role ?? session.SelectedRole));
    });

    [RelayCommand]
    private async Task ResendAsync()
    {
        if (!CanResend)
            return;

        await SendCodeAsync();
    }

    private async Task SendCodeAsync()
    {
        try
        {
            ErrorText = string.Empty;
            Destination = await auth.RequestOtpAsync(_identifier, session.SelectedRole);
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }

        OnPropertyChanged(nameof(ShowDemoHint));
        OnPropertyChanged(nameof(DemoHint));
        await StartCountdownAsync();
    }

    private async Task StartCountdownAsync()
    {
        _countdown?.Cancel();
        _countdown = new CancellationTokenSource();
        var token = _countdown.Token;

        SecondsRemaining = ResendSeconds;
        try
        {
            while (SecondsRemaining > 0)
            {
                await Task.Delay(1000, token);
                SecondsRemaining--;
            }
        }
        catch (TaskCanceledException)
        {
            // page left or code resent
        }
    }
}
