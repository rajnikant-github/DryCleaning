using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/provider/profile.</summary>
public sealed class ProviderProfileDto
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public UserDto Owner { get; set; } = new();
    public AddressDto? Address { get; set; }
    public List<CategoryDto> Categories { get; set; } = [];
    public double? RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public List<RatingDto> LatestRatings { get; set; } = [];
}
