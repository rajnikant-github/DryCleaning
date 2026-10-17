using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

/// <summary>− value + stepper with a two-way <see cref="Value"/>.</summary>
public sealed class QuantityStepper : ContentView
{
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(int), typeof(QuantityStepper), 0, BindingMode.TwoWay,
        propertyChanged: (bindable, _, value) => ((QuantityStepper)bindable)._valueLabel.Text = value?.ToString());

    public static readonly BindableProperty MinimumProperty =
        BindableProperty.Create(nameof(Minimum), typeof(int), typeof(QuantityStepper), 0);

    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(nameof(Maximum), typeof(int), typeof(QuantityStepper), 999);

    private readonly Label _valueLabel;

    public QuantityStepper()
    {
        _valueLabel = new Label
        {
            Text = "0",
            FontSize = 18,
            FontFamily = "AppSemibold",
            WidthRequest = 36,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        var minus = CreateButton("−", filled: false, () => Value = Math.Max(Minimum, Value - 1));
        var plus = CreateButton("+", filled: true, () => Value = Math.Min(Maximum, Value + 1));
        SemanticProperties.SetDescription(minus, "Decrease");
        SemanticProperties.SetDescription(plus, "Increase");

        Content = new HorizontalStackLayout { Spacing = 8, Children = { minus, _valueLabel, plus } };
        HorizontalOptions = LayoutOptions.Start;
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    private static Border CreateButton(string text, bool filled, Action onTap)
    {
        var border = new Border
        {
            WidthRequest = 40,
            HeightRequest = 40,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            StrokeThickness = filled ? 0 : 1,
            Stroke = new SolidColorBrush(Theme.Border),
            BackgroundColor = Theme.Card,
            Content = new Label
            {
                Text = text,
                FontSize = 20,
                FontFamily = "AppSemibold",
                TextColor = filled ? Colors.White : Theme.Foreground,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        if (filled)
            border.SetDynamicResource(VisualElement.BackgroundColorProperty, "Primary");

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onTap();
        border.GestureRecognizers.Add(tap);
        return border;
    }
}
