using System.Text.RegularExpressions;

namespace DryCleaning.Helpers;

/// <summary>Client-side checks that mirror the API's validation rules (so errors show before anything is sent).</summary>
public static class InputRules
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public const string PasswordHint = "At least 8 characters, with a letter and a number.";

    public static bool IsValidEmail(string? value) => !string.IsNullOrWhiteSpace(value) && EmailPattern.IsMatch(value.Trim());

    public static bool IsValidPhone(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Count(char.IsDigit) >= 9 && value.All(c => char.IsDigit(c) || " +()-".Contains(c));

    /// <summary>Same rule as the API: 8–100 characters with at least one letter and one number. Null when valid.</summary>
    public static string? PasswordError(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return "Your password needs at least 8 characters.";
        if (password.Length > 100)
            return "Your password can't be longer than 100 characters.";
        if (!password.Any(char.IsLetter))
            return "Your password needs at least one letter.";
        if (!password.Any(char.IsDigit))
            return "Your password needs at least one number.";
        return null;
    }
}
