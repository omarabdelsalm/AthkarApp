using System.Text.RegularExpressions;

namespace AthkarApp.Services;

public interface IQuranNormalizationService
{
    string Normalize(string text);
}

public class QuranNormalizationService : IQuranNormalizationService
{
    // Regular expression to match all Arabic diacritics (Harakaat)
    // 064B - 0652: Fathatan, Dammatan, Kasratan, Fatha, Damma, Kasra, Shadda, Sukun
    // 0670: Superscript Alef (Dagger Alef)
    private static readonly Regex ArabicDiacriticsRegex = new(@"[\u064B-\u0652\u0670\u0653-\u065F\u06D6-\u06ED]", RegexOptions.Compiled);

    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. Remove diacritics
        string normalized = ArabicDiacriticsRegex.Replace(text, "");

        // 2. Standardize Alefs
        normalized = Regex.Replace(normalized, "[أإآ]", "ا");

        // 3. Standardize Teh Marbuta
        normalized = Regex.Replace(normalized, "ة", "ه");

        // 4. Standardize Alef Maksura
        normalized = Regex.Replace(normalized, "ى", "ي");

        // 5. Remove extra whitespace
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }
}
