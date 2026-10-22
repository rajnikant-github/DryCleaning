using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

/// <summary>One customer rating in the provider's ratings list.</summary>
public sealed class RatingRowItem(Rating rating)
{
    public string StarsText => $"{new string('★', rating.Stars)}{new string('☆', 5 - rating.Stars)}";
    public string Who => $"{rating.CustomerName} · {rating.OrderNumber}";
    public string DateText => rating.CreatedAt.ToString("d MMM yyyy");
    public string Comment => rating.Comment ?? string.Empty;
    public bool HasComment => !string.IsNullOrWhiteSpace(rating.Comment);
}
