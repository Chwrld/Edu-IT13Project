using System.Text.RegularExpressions;

namespace MauiAppIT13.Utils;

public sealed class ValidationHelper
{
    public ValidationResult ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Invalid("Email is required.");

        if (!IsValidEmail(email))
            return ValidationResult.Invalid("Email format is invalid.");

        if (string.IsNullOrWhiteSpace(password))
            return ValidationResult.Invalid("Password is required.");

        if (password.Length < 6)
            return ValidationResult.Invalid("Password must be at least 6 characters.");

        return ValidationResult.Valid();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Valid() => new() { IsValid = true };
    public static ValidationResult Invalid(string message) => new() { IsValid = false, ErrorMessage = message };
}
