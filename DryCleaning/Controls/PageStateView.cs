using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

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
