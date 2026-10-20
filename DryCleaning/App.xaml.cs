using CommunityToolkit.Mvvm.Messaging;
using DryCleaning.Services;
using DryCleaning.Services.Push;
using DryCleaning.Services.Theming;

namespace DryCleaning;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly IRealtimeService _realtime;

    // IPushService is injected so it exists from app start and can receive a notification tap that launched the app.
    public App(AppShell shell, IRealtimeService realtime, IThemeService themes, IPushService push)
    {
        InitializeComponent();
        themes.LoadSaved();
        UserAppTheme = AppTheme.Light; // V1 design is light-only
        _shell = shell;
        _realtime = realtime;

        // Shell reads its tab bar colours once, so a resource change alone leaves the selected tab in the old colour.
        WeakReferenceMessenger.Default.Register<App, ThemeChangedMessage>(this, static (app, message) => app.RefreshTabBar(message.Theme));
    }

    private void RefreshTabBar(ColorTheme theme)
    {
        Shell.SetTabBarTitleColor(_shell, theme.Primary);
        Shell.SetTabBarForegroundColor(_shell, theme.Primary);
        Shell.SetTabBarUnselectedColor(_shell, Color.FromArgb("#6B7280"));

        // iOS only repaints the selected tab when the selection changes: hop to another tab and straight back.
        if (_shell.CurrentItem is not { } tabBar || tabBar.CurrentItem is not { } selected)
            return;
        var other = tabBar.Items.FirstOrDefault(item => item != selected);
        if (other is null)
            return;

        tabBar.CurrentItem = other;
        Dispatcher.Dispatch(() => tabBar.CurrentItem = selected);
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
