namespace DryCleaning.Api.Domain;

// Values mirror the V1 schema. Stored as strings in SQL Server.

public enum UserRole
{
    Customer,
    Provider,
    Driver,
    Admin
}
