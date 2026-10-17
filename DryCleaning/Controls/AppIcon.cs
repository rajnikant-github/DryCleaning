namespace DryCleaning.Controls;

/// <summary>
/// A glyph from the app icon font: fixed size (ignores system font scaling), theme tint, hidden from screen readers
/// unless <c>AutomationProperties.Name</c> is set. Use with names from <see cref="Helpers.AppIcons"/>.
/// </summary>
public class AppIcon : Label
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(AppIcon), string.Empty,
            propertyChanged: static (bindable, _, newValue) => ((AppIcon)bindable).Text = newValue as string ?? string.Empty);

    public AppIcon()
    {
        FontFamily = Helpers.AppIcons.Family;
        FontAutoScalingEnabled = false;
        FontSize = 24;
        HorizontalTextAlignment = TextAlignment.Center;
        VerticalTextAlignment = TextAlignment.Center;
        AutomationProperties.SetIsInAccessibleTree(this, false);
    }

    /// <summary>The glyph, e.g. <c>{x:Static icons:AppIcons.Shirt}</c>.</summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
