using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

internal static class Theme
{
    public static Color Resource(string key, string fallback)
    {
        var resources = Application.Current?.Resources;
        if (resources is not null && resources.TryGetValue(key, out var value) && value is Color color)
            return color;

        return Color.FromArgb(fallback);
    }

    public static Color Primary => Resource("Primary", "#1B3A6B");
    public static Color Border => Resource("BorderColor", "#E5E2D8");
    public static Color Muted => Resource("Muted", "#F0EDE6");
    public static Color MutedForeground => Resource("MutedForeground", "#6B7280");
    public static Color Foreground => Resource("AppForeground", "#111827");
    public static Color Card => Resource("CardColor", "#FFFFFF");
    public static Color Accent => Resource("Accent", "#0EA5E9");
}

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
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(10, 3),
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
            BackgroundColor = filled ? Theme.Primary : Theme.Card,
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
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onTap();
        border.GestureRecognizers.Add(tap);
        return border;
    }
}

/// <summary>6-box OTP input backed by a single hidden numeric Entry.</summary>
public sealed class OtpView : ContentView
{
    public static readonly BindableProperty CodeProperty = BindableProperty.Create(
        nameof(Code), typeof(string), typeof(OtpView), string.Empty, BindingMode.TwoWay,
        propertyChanged: (bindable, _, value) => ((OtpView)bindable).OnCodeChanged(value as string ?? string.Empty));

    private const int Length = 6;
    private readonly Entry _entry;
    private readonly List<(Border Box, Label Digit)> _boxes = [];

    public OtpView()
    {
        var row = new HorizontalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.Center };
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
            row.Children.Add(box);
        }

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

        var grid = new Grid { Children = { row, _entry } };
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

/// <summary>Horizontal list of photo thumbnails with an "add photo" tile. Removes on tap of ×.</summary>
public sealed class PhotoStrip : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(PhotoStrip), null,
        propertyChanged: (bindable, oldValue, newValue) => ((PhotoStrip)bindable).OnItemsSourceChanged(oldValue as IEnumerable, newValue as IEnumerable));

    public static readonly BindableProperty AddCommandProperty =
        BindableProperty.Create(nameof(AddCommand), typeof(ICommand), typeof(PhotoStrip));

    public static readonly BindableProperty AddTextProperty = BindableProperty.Create(
        nameof(AddText), typeof(string), typeof(PhotoStrip), "Add photo",
        propertyChanged: (bindable, _, _) => ((PhotoStrip)bindable).Rebuild());

    public static readonly BindableProperty AllowEditingProperty = BindableProperty.Create(
        nameof(AllowEditing), typeof(bool), typeof(PhotoStrip), true,
        propertyChanged: (bindable, _, _) => ((PhotoStrip)bindable).Rebuild());

    private readonly HorizontalStackLayout _stack = new() { Spacing = 10 };

    public PhotoStrip()
    {
        Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _stack };
        Rebuild();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? AddCommand
    {
        get => (ICommand?)GetValue(AddCommandProperty);
        set => SetValue(AddCommandProperty, value);
    }

    public string AddText
    {
        get => (string)GetValue(AddTextProperty);
        set => SetValue(AddTextProperty, value);
    }

    /// <summary>When false the strip is read-only: no add tile and no remove buttons.</summary>
    public bool AllowEditing
    {
        get => (bool)GetValue(AllowEditingProperty);
        set => SetValue(AllowEditingProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
    {
        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= OnCollectionChanged;
        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += OnCollectionChanged;
        Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        _stack.Children.Clear();

        var addTile = new Border
        {
            WidthRequest = 96,
            HeightRequest = 96,
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Stroke = new SolidColorBrush(Theme.Border),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            BackgroundColor = Theme.Card,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = "📷", FontSize = 24, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = AddText, FontSize = 12, TextColor = Theme.MutedForeground, HorizontalOptions = LayoutOptions.Center }
                }
            }
        };
        var addTap = new TapGestureRecognizer();
        addTap.Tapped += (_, _) =>
        {
            if (AddCommand?.CanExecute(null) == true)
                AddCommand.Execute(null);
        };
        addTile.GestureRecognizers.Add(addTap);
        SemanticProperties.SetDescription(addTile, AddText);
        if (AllowEditing)
            _stack.Children.Add(addTile);

        if (ItemsSource is null)
            return;

        foreach (var item in ItemsSource)
        {
            if (item is not string path)
                continue;

            var remove = new Border
            {
                WidthRequest = 24,
                HeightRequest = 24,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Color.FromArgb("#99000000"),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 4, 4, 0),
                Content = new Label { Text = "×", TextColor = Colors.White, FontSize = 16, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            var removeTap = new TapGestureRecognizer();
            removeTap.Tapped += (_, _) =>
            {
                if (ItemsSource is IList list && !list.IsReadOnly)
                    list.Remove(path);
            };
            remove.GestureRecognizers.Add(removeTap);
            SemanticProperties.SetDescription(remove, "Remove photo");

            var tile = new Border
            {
                WidthRequest = 96,
                HeightRequest = 96,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Content = new Grid
                {
                    Children =
                    {
                        new Image { Source = ImageSource.FromFile(path), Aspect = Aspect.AspectFill }
                    }
                }
            };
            if (AllowEditing && tile.Content is Grid tileGrid)
                tileGrid.Children.Add(remove);
            _stack.Children.Add(tile);
        }
    }
}

/// <summary>
/// Shows skeleton / empty / error / offline content based on a page's <see cref="ViewState"/>.
/// Hidden when the state is Content.
/// </summary>
public sealed class PageStateView : ContentView
{
    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(ViewState), typeof(PageStateView), ViewState.Loading,
        propertyChanged: (bindable, _, _) => ((PageStateView)bindable).Update());

    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(PageStateView));

    public static readonly BindableProperty EmptyIconProperty = BindableProperty.Create(
        nameof(EmptyIcon), typeof(string), typeof(PageStateView), "📋", propertyChanged: (b, _, _) => ((PageStateView)b).Update());

    public static readonly BindableProperty EmptyTitleProperty = BindableProperty.Create(
        nameof(EmptyTitle), typeof(string), typeof(PageStateView), "Nothing here yet", propertyChanged: (b, _, _) => ((PageStateView)b).Update());

    public static readonly BindableProperty EmptyMessageProperty = BindableProperty.Create(
        nameof(EmptyMessage), typeof(string), typeof(PageStateView), string.Empty, propertyChanged: (b, _, _) => ((PageStateView)b).Update());

    public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(
        nameof(ErrorMessage), typeof(string), typeof(PageStateView), "Please try again.", propertyChanged: (b, _, _) => ((PageStateView)b).Update());

    private readonly VerticalStackLayout _skeleton;

    public PageStateView()
    {
        _skeleton = BuildSkeleton();
        Update();
    }

    public ViewState State { get => (ViewState)GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }
    public string EmptyIcon { get => (string)GetValue(EmptyIconProperty); set => SetValue(EmptyIconProperty, value); }
    public string EmptyTitle { get => (string)GetValue(EmptyTitleProperty); set => SetValue(EmptyTitleProperty, value); }
    public string EmptyMessage { get => (string)GetValue(EmptyMessageProperty); set => SetValue(EmptyMessageProperty, value); }
    public string ErrorMessage { get => (string)GetValue(ErrorMessageProperty); set => SetValue(ErrorMessageProperty, value); }

    private void Update()
    {
        this.AbortAnimation("skeleton");
        IsVisible = State != ViewState.Content;

        switch (State)
        {
            case ViewState.Loading:
                Content = _skeleton;
                _skeleton.Opacity = 1;
                this.Animate("skeleton", new Animation(v => _skeleton.Opacity = v, 1, 0.45), length: 900,
                    finished: (_, _) => _skeleton.Opacity = 1, repeat: () => State == ViewState.Loading);
                break;
            case ViewState.Empty:
                Content = BuildMessage(EmptyIcon, EmptyTitle, EmptyMessage, showRetry: false);
                break;
            case ViewState.Error:
                Content = BuildMessage("⚠️", "Something went wrong", ErrorMessage, showRetry: true);
                break;
            case ViewState.Offline:
                Content = BuildMessage("📡", "No internet connection", "Check your connection and try again.", showRetry: true);
                break;
            default:
                Content = null;
                break;
        }
    }

    private View BuildMessage(string icon, string title, string message, bool showRetry)
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(32, 64),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = icon, FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                new Label { Text = title, FontSize = 18, FontFamily = "AppSemibold", HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = message, FontSize = 14, TextColor = Theme.MutedForeground, HorizontalTextAlignment = TextAlignment.Center, IsVisible = !string.IsNullOrWhiteSpace(message) }
            }
        };

        if (showRetry)
        {
            var retry = new Button { Text = "Retry", Margin = new Thickness(0, 12, 0, 0), WidthRequest = 160, HorizontalOptions = LayoutOptions.Center };
            retry.Clicked += (_, _) =>
            {
                if (RetryCommand?.CanExecute(null) == true)
                    RetryCommand.Execute(null);
            };
            layout.Children.Add(retry);
        }

        return layout;
    }

    private static VerticalStackLayout BuildSkeleton()
    {
        var layout = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(16) };
        for (var i = 0; i < 4; i++)
        {
            layout.Children.Add(new Border
            {
                BackgroundColor = Theme.Card,
                Stroke = new SolidColorBrush(Theme.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(16),
                Content = new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        Bar(0.6, 16),
                        Bar(0.4, 12),
                        Bar(0.9, 12)
                    }
                }
            });
        }

        return layout;

        static View Bar(double widthFactor, double height) => new BoxView
        {
            Color = Theme.Muted,
            HeightRequest = height,
            CornerRadius = 6,
            HorizontalOptions = LayoutOptions.Start,
            WidthRequest = 300 * widthFactor
        };
    }
}

/// <summary>Horizontally scrolling chip tabs bound to a collection of <see cref="ViewModels.Items.SegmentItem"/>.</summary>
public sealed class SegmentBar : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(SegmentBar), null,
        propertyChanged: (bindable, oldValue, newValue) => ((SegmentBar)bindable).OnItemsSourceChanged(oldValue as IEnumerable, newValue as IEnumerable));

    private readonly HorizontalStackLayout _stack = new() { Spacing = 8, Padding = new Thickness(16, 12) };
    private readonly List<ViewModels.Items.SegmentItem> _subscribed = [];

    public SegmentBar()
    {
        BackgroundColor = Theme.Card;
        Content = new VerticalStackLayout
        {
            Children =
            {
                new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _stack },
                new BoxView { HeightRequest = 1, Color = Theme.Border }
            }
        };
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
    {
        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= OnCollectionChanged;
        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += OnCollectionChanged;
        Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        foreach (var item in _subscribed)
            item.PropertyChanged -= OnItemPropertyChanged;
        _subscribed.Clear();
        _stack.Children.Clear();

        if (ItemsSource is null)
            return;

        foreach (var obj in ItemsSource)
        {
            if (obj is not ViewModels.Items.SegmentItem item)
                continue;

            var label = new Label { FontSize = 14, FontFamily = "AppSemibold", VerticalOptions = LayoutOptions.Center };
            var chip = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 1,
                Padding = new Thickness(16, 8),
                Content = label,
                BindingContext = item
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => item.SelectCommand.Execute(null);
            chip.GestureRecognizers.Add(tap);

            Apply(chip, label, item);
            item.PropertyChanged += OnItemPropertyChanged;
            _subscribed.Add(item);
            _stack.Children.Add(chip);
        }
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not ViewModels.Items.SegmentItem item)
            return;

        foreach (var child in _stack.Children)
        {
            if (child is Border { BindingContext: ViewModels.Items.SegmentItem bound } chip && ReferenceEquals(bound, item) && chip.Content is Label label)
                Apply(chip, label, item);
        }
    }

    private static void Apply(Border chip, Label label, ViewModels.Items.SegmentItem item)
    {
        label.Text = item.HasCount ? $"{item.Title}  {item.Count}" : item.Title;
        label.TextColor = item.IsSelected ? Colors.White : Theme.MutedForeground;
        chip.BackgroundColor = item.IsSelected ? Theme.Primary : Theme.Card;
        chip.Stroke = new SolidColorBrush(item.IsSelected ? Theme.Primary : Theme.Border);
        SemanticProperties.SetDescription(chip, item.IsSelected ? $"{item.Title}, selected" : item.Title);
    }
}
