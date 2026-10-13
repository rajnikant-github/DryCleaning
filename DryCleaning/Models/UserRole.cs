namespace DryCleaning.Models;

// Enum names/values mirror the V1 database schema so they serialize 1:1 with the API.

public enum UserRole
{
    Customer,
    Provider,
    Driver,
    Admin
}
