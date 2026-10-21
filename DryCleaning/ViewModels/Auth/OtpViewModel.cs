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
    private int _resendSeconds = 30;
    private bool _isRegistration;
    private bool _started;
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
    public string ResendText => CanResend ? "Resend code" : $"Resend code in {SecondsRemaining}s";
    public bool ShowDemoHint => !string.IsNullOrEmpty(auth.OtpHint);
    public string DemoHint => $"Demo code: {auth.OtpHint}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.IdentifierKey, out var value) && value is string identifier)
            _identifier = identifier;

        // Confirming a new sign-up: the code was already emailed on the previous screen.
        if (query.TryGetValue(Routes.PurposeKey, out var purpose) && purpose is string p && p == Routes.RegisterPurpose)
        {
            _isRegistration = true;
            _resendSeconds = query.TryGetValue(Routes.ResendSecondsKey, out var seconds) && seconds is int s ? s : 60;
            if (query.TryGetValue(Routes.DestinationKey, out var destination) && destination is string d)
                Destination = d;
        }
    }

    public override async Task OnAppearingAsync()
    {
        State = ViewState.Content;
        if (_isRegistration)
        {
            if (!_started)
            {
                _started = true;
                OnPropertyChanged(nameof(ShowDemoHint));
                OnPropertyChanged(nameof(DemoHint));
                await StartCountdownAsync();
            }
            return;
        }

        if (string.IsNullOrEmpty(Destination))
            await SendCodeAsync();
    }

    private bool CanVerify() => Code.Length == 6;

    [RelayCommand(CanExecute = nameof(CanVerify))]
    private Task VerifyAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        var result = _isRegistration
            ? await auth.ConfirmRegistrationAsync(_identifier, Code)
            : await auth.VerifyOtpAsync(_identifier, Code, session.SelectedRole);
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
            if (_isRegistration)
            {
                var resent = await auth.ResendRegistrationCodeAsync(_identifier);
                if (!resent.Success)
                {
                    ErrorText = resent.Error ?? "We couldn't send a new code.";
                    return;
                }

                Destination = resent.Destination;
                _resendSeconds = resent.ResendInSeconds;
            }
            else
            {
                Destination = await auth.RequestOtpAsync(_identifier, session.SelectedRole);
            }
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

        SecondsRemaining = _resendSeconds;
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
