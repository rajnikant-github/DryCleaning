using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Every customer rating of this provider, newest first, loaded a page at a time.</summary>
public sealed partial class RatingsViewModel(ViewModelServices services, IProviderProfileService profiles) : BaseViewModel(services)
{
    private const int PageSize = 20;
    private int _page;
    private bool _hasMore;
    private bool _loadingMore;

    public ObservableCollection<RatingRowItem> Ratings { get; } = [];

    [ObservableProperty] private string summaryText = string.Empty;
    [ObservableProperty] private string averageText = string.Empty;
    [ObservableProperty] private bool onlyWithComment;

    partial void OnOnlyWithCommentChanged(bool value) => _ = RunBusyAsync(async () => await LoadAsync(showSkeleton: false));

    protected override async Task<bool> LoadDataAsync()
    {
        var summary = (await profiles.GetProfileAsync()).Ratings;
        AverageText = summary.Average is { } average && summary.Count > 0 ? $"{average:0.0}" : "–";
        SummaryText = summary.Count == 0
            ? "No ratings yet"
            : $"{summary.Count} {(summary.Count == 1 ? "rating" : "ratings")} from customers";

        _page = 1;
        var page = await profiles.GetRatingsAsync(_page, PageSize, OnlyWithComment);
        Ratings.Clear();
        foreach (var rating in page.Items)
            Ratings.Add(new RatingRowItem(rating));
        _hasMore = page.HasMore;
        return Ratings.Count > 0;
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!_hasMore || _loadingMore)
            return;

        _loadingMore = true;
        try
        {
            var page = await profiles.GetRatingsAsync(_page + 1, PageSize, OnlyWithComment);
            _page++;
            foreach (var rating in page.Items)
                Ratings.Add(new RatingRowItem(rating));
            _hasMore = page.HasMore;
        }
        finally
        {
            _loadingMore = false;
        }
    }
}
