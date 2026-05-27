using AmazingBot;
using System.Text.RegularExpressions;
using Xceed.Document.NET;

internal static class AlignmentMapper
{
    public static Alignment GetAlignment(string alignmentText) =>
     alignmentText.ToLower() switch
     {
         "left" => Alignment.left,
         "center" => Alignment.center,
         "right" => Alignment.right,
         "both" => Alignment.both,
         _ => Alignment.center
     };

    public static string RemoveParenthesesContent(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return Regex.Replace(input, @"\s*\(.*?\)", "").Trim();
    }

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    public static string GetComplaintWord(int count)
    {
        int mod10 = count % 10;
        int mod100 = count % 100;

        if (mod100 >= 11 && mod100 <= 19) return "жалоб";
        if (mod10 == 1) return "жалоба";
        if (mod10 >= 2 && mod10 <= 4) return "жалобы";
        return "жалоб";
    }
}
