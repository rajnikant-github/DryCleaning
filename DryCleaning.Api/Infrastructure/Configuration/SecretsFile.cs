using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;

namespace DryCleaning.Shared.Secrets;

/// <summary>
/// Reads the repository's single git-ignored <c>secrets.json</c> (see <c>secrets.example.json</c>). The file has one block per
/// environment (<c>local</c>, <c>development</c>, <c>production</c>) and, inside it, one section per project (<c>Api</c>, <c>Admin</c>, <c>App</c>).
/// A project adds its own section as configuration: above appsettings*.json and user-secrets, below environment variables,
/// so real server environment variables still win in production.
/// This file is linked into the admin portal, so both projects share one implementation.
/// </summary>
public static class SecretsFile
{
    public const string FileName = "secrets.json";

    /// <summary>Overrides where the file is looked for (otherwise: the project folder and its parents).</summary>
    public const string PathVariable = "EDRY_SECRETS_FILE";

    /// <summary>Overrides which environment block is used (otherwise it follows ASPNETCORE_ENVIRONMENT).</summary>
    public const string EnvironmentVariable = "EDRY_ENVIRONMENT";

    /// <summary>Adds the <paramref name="section"/> ("Api", "Admin") of the current environment's block, if the file exists.</summary>
    public static void AddSecretsFile(this ConfigurationManager configuration, IHostEnvironment environment, string section)
    {
        var path = Locate(environment.ContentRootPath);
        if (path is null)
            return;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{path} is not valid JSON: {ex.Message}");
        }

        using (document)
        {
            var block = EnvironmentBlock(environment);
            if (!document.RootElement.TryGetProperty(block, out var env) || !env.TryGetProperty(section, out var values) || values.ValueKind != JsonValueKind.Object)
                return;

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            Flatten(values, prefix: null, data);
            if (data.Count == 0)
                return;

            // Just before the app's environment-variables source (the last one; an earlier one only carries host settings),
            // so this file wins over appsettings*.json and user-secrets while real environment variables keep the last word.
            var index = configuration.Sources.ToList().FindLastIndex(s => s is EnvironmentVariablesConfigurationSource);
            var source = new MemoryConfigurationSource { InitialData = data };
            if (index >= 0)
                configuration.Sources.Insert(index, source);
            else
                configuration.Sources.Add(source);
        }
    }

    /// <summary>The full path of the secrets file in use, or null when there is none.</summary>
    public static string? FindPath(IHostEnvironment environment) => Locate(environment.ContentRootPath);

    /// <summary>The block name for the current environment: Development → local.</summary>
    public static string EnvironmentBlock(IHostEnvironment environment)
    {
        var overridden = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overridden))
            return overridden.Trim();

        return environment.EnvironmentName.ToLowerInvariant() switch
        {
            "development" => "local",
            var name => name
        };
    }

    private static string? Locate(string startFolder)
    {
        var explicitPath = Environment.GetEnvironmentVariable(PathVariable);
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return File.Exists(explicitPath) ? Path.GetFullPath(explicitPath) : null;

        // The API and the portal run from their own folders; the file sits at the repository root.
        var folder = new DirectoryInfo(startFolder);
        for (var depth = 0; folder is not null && depth < 5; depth++, folder = folder.Parent)
        {
            var candidate = Path.Combine(folder.FullName, FileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Turns nested JSON into configuration keys ("Paystack:SecretKey"). A property whose name ends in "Json" keeps its
    /// object as raw JSON text instead (e.g. the Firebase service-account file pasted in as an object).
    /// </summary>
    private static void Flatten(JsonElement element, string? prefix, Dictionary<string, string?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = prefix is null ? property.Name : $"{prefix}:{property.Name}";
                    if (property.Name.EndsWith("Json", StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        data[key] = property.Value.GetRawText();
                    else
                        Flatten(property.Value, key, data);
                }

                break;
            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                    Flatten(item, $"{prefix}:{i++}", data);
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                break;
            default:
                if (prefix is not null)
                    data[prefix] = element.ValueKind is JsonValueKind.String ? element.GetString() : element.GetRawText();
                break;
        }
    }
}
