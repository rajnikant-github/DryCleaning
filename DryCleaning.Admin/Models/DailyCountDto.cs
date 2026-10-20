namespace DryCleaning.Admin.Models;

public sealed class DailyCountDto
{
    public DateOnly Date { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}
