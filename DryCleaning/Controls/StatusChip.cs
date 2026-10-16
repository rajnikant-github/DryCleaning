using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

/// <summary>Coloured pill showing an <see cref="OrderStatus"/>.</summary>
public sealed class StatusChip : ContentView
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(OrderStatus), typeof(StatusChip), OrderStatus.Created,
        propertyChanged: (bindable, _, _) => ((StatusChip)bindable).Update());

    private readonly Label _label;
    private readonly Border _border;

    public StatusChip()
    {
        _label = new Label { FontSize = 11, FontFamily = "AppSemibold", VerticalTextAlignment = TextAlignment.Center };
        _border = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 5),
            Content = _label
        };
        Content = _border;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Update();
    }

    public OrderStatus Status
    {
        get => (OrderStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    private void Update()
    {
        var (background, text) = StatusPalette.For(Status);
        _border.BackgroundColor = background;
        _label.TextColor = text;
        _label.Text = Status.DisplayName();
        SemanticProperties.SetDescription(this, $"Status: {Status.DisplayName()}");
    }
}
