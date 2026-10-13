using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

public sealed class ApiSettings
{
    public string BaseUrl { get; set; } = "https://edrycleaners.co.za/api/";
}
