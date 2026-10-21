using DryCleaning.Models;
using Microsoft.Maui.Controls.Shapes;

namespace DryCleaning.Controls;

/// <summary>
/// Notice shown at the top of a provider's or driver's Profile page while an admin hasn't approved the account yet
/// (or after it was deactivated). Hidden when the account is active.
/// </summary>
public sealed class AccountStatusCard : ContentView
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(ProfileStatus), typeof(AccountStatusCard), ProfileStatus.Active,
        propertyChanged: (b, _, _) => ((AccountStatusCard)b).Update());

    public static readonly BindableProperty IsDriverProperty = BindableProperty.Create(
        nameof(IsDriver), typeof(bool), typeof(AccountStatusCard), false,
        propertyChanged: (b, _, _) => ((AccountStatusCard)b).Update());

    private readonly Border _border;
    private readonly Label _icon;
    private readonly Label _title;
    private readonly Label _message;

    public AccountStatusCard()
    {
        _icon = new Label { FontFamily = "FA", FontSize = 22, VerticalOptions = LayoutOptions.Start };
        _title = new Label { FontFamily = "AppSemibold", FontSize = 15 };
        _message = new Label { FontSize = 13 };

        var text = new VerticalStackLayout { Spacing = 4, Children = { _title, _message } };
        var row = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            ColumnSpacing = 12
        };
        row.Add(_icon);
        row.Add(text, 1);

        _border = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(14),
            Content = row
        };
        Content = _border;
        Update();
    }

    public ProfileStatus Status
    {
        get => (ProfileStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public bool IsDriver
    {
        get => (bool)GetValue(IsDriverProperty);
        set => SetValue(IsDriverProperty, value);
    }

    private void Update()
    {
        IsVisible = Status != ProfileStatus.Active;
        if (!IsVisible)
            return;

        var deactivated = Status == ProfileStatus.Deactivated;
        var (background, stroke, text) = deactivated
            ? (Color.FromArgb("#FEF2F2"), Color.FromArgb("#FECACA"), Color.FromArgb("#B91C1C"))
            : (Color.FromArgb("#FFFBEB"), Color.FromArgb("#FDE68A"), Color.FromArgb("#92400E"));

        _border.BackgroundColor = background;
        _border.Stroke = stroke;
        _icon.TextColor = text;
        _title.TextColor = text;
        _message.TextColor = text;

        if (deactivated)
        {
            _icon.Text = "";
            _title.Text = "Your account is deactivated";
            _message.Text = "An admin has switched your account off. Please contact E Dry Cleaners support for help.";
            SemanticProperties.SetDescription(this, $"{_title.Text}. {_message.Text}");
            return;
        }

        _icon.Text = "";
        _title.Text = "Your account is under review";
        _message.Text = IsDriver
            ? "Our team is reviewing your driver account. You can update your details below. You'll get a notification as soon as it's approved, and then you can go online to accept jobs."
            : "Our team is reviewing your business. You can finish your profile below. You'll get a notification as soon as it's approved, and then your business goes live for customers.";
        SemanticProperties.SetDescription(this, $"{_title.Text}. {_message.Text}");
    }
}
