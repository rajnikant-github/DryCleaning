using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

public sealed class PredictionItem(PlacePrediction prediction, Func<PlacePrediction, Task> select)
{
    public string MainText => prediction.MainText;
    public string SecondaryText => prediction.SecondaryText;
    public IAsyncRelayCommand SelectCommand { get; } = new AsyncRelayCommand(() => select(prediction));
}
