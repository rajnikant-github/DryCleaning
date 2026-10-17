namespace DryCleaning.Services.Theming;

/// <summary>Sent when the user picks another app colour; controls that paint themselves in code redraw.</summary>
public sealed class ThemeChangedMessage(ColorTheme theme)
{
    public ColorTheme Theme { get; } = theme;
}
