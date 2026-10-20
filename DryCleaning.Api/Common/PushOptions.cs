using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

/// <summary>Section "Push": Firebase Cloud Messaging (Android + iOS).</summary>
public sealed class PushOptions
{
    public const string Section = "Push";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path to the Firebase service-account JSON (Firebase console → Project settings → Service accounts).
    /// When empty, pushes are only logged.
    /// </summary>
    public string? FirebaseCredentialsPath { get; set; }

    /// <summary>The Firebase service-account JSON itself (as kept in secrets.json). Used instead of <see cref="FirebaseCredentialsPath"/> when set.</summary>
    public string? FirebaseCredentialsJson { get; set; }
}
