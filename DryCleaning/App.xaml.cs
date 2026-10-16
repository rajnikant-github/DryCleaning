using DryCleaning.Services;

namespace DryCleaning;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly IRealtimeService _realtime;

    public App(AppShell shell, IRealtimeService realtime)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light; // V1 design is light-only
        _shell = shell;
        _realtime = realtime;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell);
        // Live updates only while the app is on screen; push notifications cover the background.
        window.Activated += (_, _) => _ = _realtime.StartAsync();
        window.Stopped += (_, _) => _ = _realtime.StopAsync();
        return window;
    }
}
