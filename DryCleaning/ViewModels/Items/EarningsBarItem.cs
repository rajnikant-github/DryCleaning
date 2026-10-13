using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

/// <summary>One bar of the earnings chart (D13).</summary>
public sealed class EarningsBarItem(EarningsBar bar, decimal max)
{
    private const double MaxBarHeight = 120;

    public string Label => bar.Label;
    public decimal Amount => bar.Amount;
    public double BarHeight => max <= 0 ? 4 : Math.Max(4, (double)(bar.Amount / max) * MaxBarHeight);
    public bool HasAmount => bar.Amount > 0;
    public bool IsEmpty => bar.Amount <= 0;
}
