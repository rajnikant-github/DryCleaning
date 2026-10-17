namespace DryCleaning.Services.Theming;

/// <summary>One selectable set of app colours.</summary>
public sealed class ColorTheme(string key, string name, string primary, string primaryLight, string secondary)
{
    /// <summary>Stored on the phone to remember the choice.</summary>
    public string Key { get; } = key;

    public string Name { get; } = name;

    /// <summary>Headers, buttons and highlights.</summary>
    public Color Primary { get; } = Color.FromArgb(primary);

    /// <summary>Light text on a <see cref="Primary"/> background (captions, unselected tabs).</summary>
    public Color PrimaryLight { get; } = Color.FromArgb(primaryLight);

    /// <summary>Very light tint of the primary colour for soft panels and secondary buttons.</summary>
    public Color Secondary { get; } = Color.FromArgb(secondary);
}
