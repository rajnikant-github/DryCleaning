namespace DryCleaning.Services.Theming;

public static class ColorThemes
{
    public static ColorTheme Navy { get; } = new("navy", "Navy", "#1B3A6B", "#BFDBFE", "#EEF4FF", "#142C50", "#315B94");

    public static ColorTheme Coral { get; } = new("coral", "Coral", "#F24A4A", "#FFD6D6", "#FFEEEE", "#D9363E", "#FF7067");

    public static ColorTheme Mint { get; } = new(
        "mint", "Mint", "#3D7A46", "#DDF2DF", "#EEF8EF", "#326A3C", "#4F8D58", "#ACE1AF");

    public static IReadOnlyList<ColorTheme> All { get; } = [Navy, Coral, Mint];

    public static ColorTheme ByKey(string? key) => All.FirstOrDefault(t => t.Key == key) ?? Navy;
}
