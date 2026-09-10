using System.Text.RegularExpressions;

namespace CareerPilot.Api.services;

public static class AuthValidation
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string? ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email is required.";
        if (!EmailRegex.IsMatch(email.Trim()))
            return "Enter a valid email address.";
        return null;
    }

    public static string? ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "Password is required.";
        if (password.Length < 8)
            return "Password must be at least 8 characters long.";
        if (!password.Any(char.IsUpper))
            return "Password must contain an uppercase letter.";
        if (!password.Any(char.IsLower))
            return "Password must contain a lowercase letter.";
        if (!password.Any(char.IsDigit))
            return "Password must contain a number.";
        if (password.All(char.IsLetterOrDigit))
            return "Password must contain a symbol (e.g. ! ? @ # $).";
        return null;
    }
}
