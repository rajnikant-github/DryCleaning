using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

/// <summary>Builds the order timeline (happy path, or reached steps + terminal status).</summary>
public static class TimelineBuilder
{
    public static List<TimelineStep> Build(OrderDetail detail, Func<OrderStatus, string> title)
    {
        var status = detail.Order.Status;
        var reached = detail.History.Select(h => h.Status).ToHashSet();
        var terminal = status is OrderStatus.Cancelled or OrderStatus.Disputed;

        List<OrderStatus> steps;
        if (terminal)
        {
            steps = OrderWorkflow.Lifecycle.Where(reached.Contains).ToList();
            steps.Add(status);
        }
        else
        {
            steps = OrderWorkflow.Lifecycle.ToList();
        }

        var currentIndex = steps.IndexOf(status);
        var result = new List<TimelineStep>();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var history = detail.History.LastOrDefault(h => h.Status == step);
            var isCurrent = i == currentIndex;
            result.Add(new TimelineStep
            {
                Title = title(step),
                TimeText = history is null ? string.Empty : history.CreatedAt.ToString("d MMM, HH:mm"),
                Notes = isCurrent || terminal ? history?.Notes : null,
                IsDone = i < currentIndex || (isCurrent && step == OrderStatus.Completed),
                IsActive = isCurrent && !terminal && step != OrderStatus.Completed,
                IsAlert = isCurrent && terminal,
                IsLast = i == steps.Count - 1
            });
        }

        return result;
    }
}
