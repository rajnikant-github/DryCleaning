using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

/// <summary>Round initials avatar.</summary>
public sealed class AvatarView : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(AvatarView), string.Empty,
        propertyChanged: (bindable, _, value) => ((AvatarView)bindable)._label.Text = value as string);

    public static readonly BindableProperty AvatarSizeProperty = BindableProperty.Create(
        nameof(AvatarSize), typeof(double), typeof(AvatarView), 44d,
        propertyChanged: (bindable, _, _) => ((AvatarView)bindable).ApplySize());

    private readonly Label _label;
    private readonly Border _border;

    public AvatarView()
    {
        _label = new Label
        {
            TextColor = Colors.White,
            FontFamily = "AppSemibold",
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        _border = new Border { StrokeThickness = 0, BackgroundColor = Theme.Primary, Content = _label };
        Content = _border;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        ApplySize();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double AvatarSize
    {
        get => (double)GetValue(AvatarSizeProperty);
        set => SetValue(AvatarSizeProperty, value);
    }

    private void ApplySize()
    {
        _border.WidthRequest = AvatarSize;
        _border.HeightRequest = AvatarSize;
        _border.StrokeShape = new RoundRectangle { CornerRadius = AvatarSize / 2 };
        _label.FontSize = AvatarSize * 0.36;
    }
}
