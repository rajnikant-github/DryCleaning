using CommunityToolkit.Mvvm.Messaging;
using DryCleaning.Helpers;
using DryCleaning.Services;
using DryCleaning.Services.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace DryCleaning.Controls;

/// <summary>Profile-page card "App colour": one tappable swatch per theme; the choice applies instantly and is remembered.</summary>
public sealed class ThemePickerView : ContentView
{
    private readonly HorizontalStackLayout _swatches = new() { Spacing = 20 };
    private IThemeService? _themes;

    public ThemePickerView()
    {
        var card = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Theme.Border),
            BackgroundColor = Theme.Card,
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "APP COLOUR", FontSize = 12, FontFamily = "AppSemibold", TextColor = Theme.MutedForeground, CharacterSpacing = 1 },
                    _swatches,
                    new Label { Text = "Changes the colour of headers, buttons and highlights.", FontSize = 12, TextColor = Theme.MutedForeground }
                }
            }
        };
        Content = card;

        WeakReferenceMessenger.Default.Register<ThemePickerView, ThemeChangedMessage>(this, (view, _) => view.Build());
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _themes ??= Handler?.MauiContext?.Services.GetService<IThemeService>();
        Build();
    }

    private void Build()
    {
        _swatches.Children.Clear();
        if (_themes is null)
            return;

        foreach (var theme in _themes.Themes)
        {
            var selected = theme.Key == _themes.Current.Key;
            var circle = new Border
            {
                WidthRequest = 48,
                HeightRequest = 48,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                BackgroundColor = theme.Primary,
                StrokeThickness = selected ? 3 : 0,
                Stroke = new SolidColorBrush(Theme.Foreground),
                Content = selected
                    ? new Label
                    {
                        Text = AppIcons.Check,
                        FontFamily = AppIcons.Family,
                        FontSize = 18,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                    : null
            };

            var item = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    circle,
                    new Label
                    {
                        Text = theme.Name,
                        FontSize = 13,
                        FontFamily = selected ? "AppSemibold" : "AppRegular",
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => _themes.Apply(theme);
            item.GestureRecognizers.Add(tap);
            SemanticProperties.SetDescription(item, selected ? $"{theme.Name} colour, selected" : $"{theme.Name} colour");
            _swatches.Children.Add(item);
        }
    }
}
