using System.Text.RegularExpressions;

public static class InputValidator
{
    private static readonly Regex NameRegex = new Regex(@"^[a-zA-Zа-яА-Я\s]{2,15}$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_]{3,15}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex SafeRegex = new Regex(@"[<>""'&;]", RegexOptions.Compiled);

    public static bool IsValidName(string name) =>
        !string.IsNullOrEmpty(name) && NameRegex.IsMatch(name);

    public static bool IsValidUsername(string username) =>
        !string.IsNullOrEmpty(username) && UsernameRegex.IsMatch(username);

    public static bool IsValidEmail(string email) =>
        !string.IsNullOrEmpty(email) && EmailRegex.IsMatch(email);

    public static bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        bool hasLetter = Regex.IsMatch(password, @"[a-zA-Z]");
        bool hasDigit = Regex.IsMatch(password, @"\d");
        bool hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]");

        return hasLetter && (hasDigit || hasSpecial);
    }

    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return SafeRegex.Replace(input, "");
    }
}