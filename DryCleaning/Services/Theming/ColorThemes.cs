namespace DryCleaning.Services.Theming;

public static class ColorThemes
{
    public static ColorTheme Navy { get; } = new("navy", "Navy", "#1B3A6B", "#BFDBFE", "#EEF4FF");

    public static ColorTheme Coral { get; } = new("coral", "Coral", "#F24A4A", "#FFD6D6", "#FFEEEE");

    public static IReadOnlyList<ColorTheme> All { get; } = [Navy, Coral];

    public static ColorTheme ByKey(string? key) => All.FirstOrDefault(t => t.Key == key) ?? Navy;
}
