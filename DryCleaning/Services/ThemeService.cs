using CommunityToolkit.Mvvm.Messaging;
using DryCleaning.Services.Theming;

namespace DryCleaning.Services;

public sealed class ThemeService : IThemeService
{
    private const string PreferenceKey = "edc.app.theme";

    public ColorTheme Current { get; private set; } = ColorThemes.Coral;

    public IReadOnlyList<ColorTheme> Themes => ColorThemes.All;

    public void LoadSaved() => Apply(ColorThemes.ByKey(SafeGet()), save: false);

    public void Apply(ColorTheme theme) => Apply(theme, save: true);

    private void Apply(ColorTheme theme, bool save)
    {
        Current = theme;

        // Values set here win over the defaults in Colors.xaml, and every {DynamicResource ...} user updates itself.
        if (Application.Current?.Resources is { } resources)
        {
            resources["Primary"] = theme.Primary;
            resources["PrimaryBrush"] = new SolidColorBrush(theme.Primary);
            resources["PrimaryLight"] = theme.PrimaryLight;
            resources["PrimaryLightBrush"] = new SolidColorBrush(theme.PrimaryLight);
            resources["SecondaryColor"] = theme.Secondary;
        }

        if (save)
        {
            try
            {
                Preferences.Default.Set(PreferenceKey, theme.Key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Theme] Could not save: {ex.Message}");
            }
        }

        WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(theme));
    }

    private static string? SafeGet()
    {
        try
        {
            return Preferences.Default.Get<string?>(PreferenceKey, null);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
