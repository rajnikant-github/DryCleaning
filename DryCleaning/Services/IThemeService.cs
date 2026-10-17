using DryCleaning.Services.Theming;

namespace DryCleaning.Services;

/// <summary>The app's colour theme: applies it instantly and remembers it on the phone.</summary>
public interface IThemeService
{
    ColorTheme Current { get; }
    IReadOnlyList<ColorTheme> Themes { get; }

    /// <summary>Applies the saved theme (call once at start-up).</summary>
    void LoadSaved();

    /// <summary>Applies and saves a theme; screens update straight away.</summary>
    void Apply(ColorTheme theme);
}
