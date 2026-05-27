internal static class StringExtensions
{
    public static string RemoveLeadingNumber(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var parts = text.Split('.', 2);

        if (parts.Length < 2 || !parts[0].All(char.IsDigit))
            return text;

        return parts[1].TrimStart();
    }
}
