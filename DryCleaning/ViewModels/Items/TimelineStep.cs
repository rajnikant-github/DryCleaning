using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

public sealed class TimelineStep
{
    public string Title { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool IsDone { get; init; }
    public bool IsActive { get; init; }
    public bool IsAlert { get; init; }
    public bool IsLast { get; init; }

    public bool HasTime => !string.IsNullOrEmpty(TimeText);
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool ShowLine => !IsLast;
    public string Marker => IsAlert ? Helpers.AppIcons.Exclamation : IsDone ? Helpers.AppIcons.Check : string.Empty;
}
