using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip)]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}
