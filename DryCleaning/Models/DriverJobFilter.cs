namespace DryCleaning.Models;

// Read models for the Driver app (shape of the /api/driver responses).

public enum DriverJobFilter
{
    Available,   // Pending jobs on the marketplace
    Mine,        // Assigned / InProgress
    Completed
}
