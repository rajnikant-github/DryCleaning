namespace DryCleaning.Services.Theming;

public static class ColorThemes
{
    public static ColorTheme Coral { get; } = new("coral", "Coral", "#FF725E", "#FFE9E5", "#FFF1EE");

    public static ColorTheme Rose { get; } = new("rose", "Rose", "#E67565", "#FFE3DD", "#FDF0ED");

    public static IReadOnlyList<ColorTheme> All { get; } = [Coral, Rose];

    public static ColorTheme ByKey(string? key) => All.FirstOrDefault(t => t.Key == key) ?? Coral;
}
