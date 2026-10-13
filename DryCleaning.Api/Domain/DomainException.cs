namespace DryCleaning.Api.Domain;

/// <summary>A business rule was violated (mapped to HTTP 409).</summary>
public class DomainException(string message) : Exception(message);
