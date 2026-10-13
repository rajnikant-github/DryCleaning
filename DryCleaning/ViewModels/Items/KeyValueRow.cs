using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

/// <summary>A label/value row (e.g. address fields).</summary>
public sealed class KeyValueRow
{
    public required string Label { get; init; }
    public required string Value { get; init; }
}
