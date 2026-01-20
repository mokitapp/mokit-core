using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mokit.Application.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        var slug = text.ToLowerInvariant();

        // Remove diacritics (accents)
        slug = RemoveDiacritics(slug);

        // Replace Turkish characters
        slug = ReplaceTurkishChars(slug);

        // Replace spaces and invalid chars with hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // Limit length
        if (slug.Length > 50)
            slug = slug.Substring(0, 50).TrimEnd('-');

        return slug;
    }

    public static string GenerateUniqueSlug(string text, Func<string, bool> slugExists)
    {
        var baseSlug = GenerateSlug(text);
        var slug = baseSlug;
        var counter = 1;

        while (slugExists(slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string ReplaceTurkishChars(string text)
    {
        var replacements = new Dictionary<char, char>
        {
            { 'ı', 'i' },
            { 'ğ', 'g' },
            { 'ü', 'u' },
            { 'ş', 's' },
            { 'ö', 'o' },
            { 'ç', 'c' },
            { 'İ', 'i' },
            { 'Ğ', 'g' },
            { 'Ü', 'u' },
            { 'Ş', 's' },
            { 'Ö', 'o' },
            { 'Ç', 'c' }
        };

        var result = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            result.Append(replacements.TryGetValue(c, out var replacement) ? replacement : c);
        }

        return result.ToString();
    }
}

