using System.Text.Json.Serialization;

namespace DryCleaning.Services.Places;

/// <summary>Source-generated JSON for the Google Places request body (trim/AOT safe).</summary>
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(string))]
internal sealed partial class PlacesJsonContext : JsonSerializerContext
{
}
