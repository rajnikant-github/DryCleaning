using DryCleaning.Services.Theming;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

/// <summary>OTP input (6 boxes by default) backed by a single hidden numeric Entry.</summary>
public sealed class OtpView : ContentView
{
    public static readonly BindableProperty CodeProperty = BindableProperty.Create(
        nameof(Code), typeof(string), typeof(OtpView), string.Empty, BindingMode.TwoWay,
        propertyChanged: (bindable, _, value) => ((OtpView)bindable).OnCodeChanged(value as string ?? string.Empty));

    public static readonly BindableProperty LengthProperty = BindableProperty.Create(
        nameof(Length), typeof(int), typeof(OtpView), 6,
        propertyChanged: (bindable, _, _) => ((OtpView)bindable).BuildBoxes());

    private readonly Entry _entry;
    private readonly HorizontalStackLayout _row = new() { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
    private readonly List<(Border Box, Label Digit)> _boxes = [];

    public int Length
    {
        get => (int)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    private void BuildBoxes()
    {
        _boxes.Clear();
        _row.Children.Clear();
        for (var i = 0; i < Length; i++)
        {
            var digit = new Label
            {
                FontSize = 22,
                FontFamily = "AppSemibold",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            var box = new Border
            {
                WidthRequest = 46,
                HeightRequest = 54,
                StrokeThickness = 2,
                Stroke = new SolidColorBrush(Theme.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Theme.Card,
                Content = digit
            };
            _boxes.Add((box, digit));
            _row.Children.Add(box);
        }

        if (_entry is not null)
        {
            _entry.MaxLength = Length;
            OnCodeChanged(Code ?? string.Empty);
        }
    }

    public OtpView()
    {
        WeakReferenceMessenger.Default.Register<OtpView, ThemeChangedMessage>(this, (view, _) => view.Highlight());
        BuildBoxes();
        _entry = new Entry
        {
            Keyboard = Keyboard.Numeric,
            MaxLength = Length,
            Opacity = 0.02,
            TextColor = Colors.Transparent,
            BackgroundColor = Colors.Transparent,
            HeightRequest = 54
        };
        _entry.TextChanged += (_, e) =>
        {
            var digits = new string((e.NewTextValue ?? string.Empty).Where(char.IsDigit).Take(Length).ToArray());
            if (digits != e.NewTextValue)
            {
                _entry.Text = digits;
                return;
            }

            Code = digits;
        };
        _entry.Focused += (_, _) => Highlight();
        _entry.Unfocused += (_, _) => Highlight();
        SemanticProperties.SetDescription(_entry, "One-time code");

        var grid = new Grid { Children = { _row, _entry } };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => _entry.Focus();
        grid.GestureRecognizers.Add(tap);
        Content = grid;
        Highlight();
    }

    public string Code
    {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    public void FocusInput() => _entry.Focus();

    private void OnCodeChanged(string code)
    {
        if (_entry.Text != code)
            _entry.Text = code;

        for (var i = 0; i < Length; i++)
            _boxes[i].Digit.Text = i < code.Length ? code[i].ToString() : string.Empty;

        Highlight();
    }

    private void Highlight()
    {
        var current = Math.Min(Code?.Length ?? 0, Length - 1);
        for (var i = 0; i < Length; i++)
        {
            var active = _entry.IsFocused && i == current;
            var filled = i < (Code?.Length ?? 0);
            _boxes[i].Box.Stroke = new SolidColorBrush(active || filled ? Theme.Primary : Theme.Border);
        }
    }
}
