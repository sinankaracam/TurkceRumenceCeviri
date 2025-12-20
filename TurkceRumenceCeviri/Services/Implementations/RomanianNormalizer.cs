using System;
using System.Text.RegularExpressions;

namespace TurkceRumenceCeviri.Services.Implementations;

public static class RomanianNormalizer
{
    // Detect language based on diacritics and common stopwords/patterns
    public static string DetectLanguageForText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "tr";

        var roScore = 0;
        var trScore = 0;

        // Diacritics scoring using sets (avoids duplicate-case issues)
        var roChars = new HashSet<char>(new[] { '?','?','â','Â','î','Î','?','?','?','?' });
        var trChars = new HashSet<char>(new[] { 'ð','Ð','ý','Ý','þ','Þ','ç','Ç','ö','Ö','ü','Ü' });
        foreach (var ch in text)
        {
            if (roChars.Contains(ch)) roScore += 3;
            if (trChars.Contains(ch)) trScore += 3;
        }

        // Romanian common words (exclude ambiguous words like 'din')
        var roWords = new[] { "?i", "este", "sunt", "în", "cu", "la", "de", "?sta", "acesta", "românia", "bucure?ti" };
        foreach (var w in roWords)
            roScore += CountOccurrences(text, w) * 2;

        // Turkish common words (exclude ambiguous 'de'/'da')
        var trWords = new[] { "ve", "bir", "için", "çok", "ama", "ile", "þu", "bu" };
        foreach (var w in trWords)
            trScore += CountOccurrences(text, w) * 2;

        // Heuristics: punctuation patterns
        if (Regex.IsMatch(text, "\\b(Þ|Ç|Ý)[a-z]")) trScore += 1; // Turkish uppercase with accents
        if (Regex.IsMatch(text, "\\b(?|?|?|Â|Î)[a-z]")) roScore += 1; // Romanian uppercase with accents

        return roScore >= trScore ? "ro" : "tr";
    }

    public static bool IsLikelyTurkish(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var trChars = new HashSet<char>(new[] { 'ð','Ð','ý','Ý','þ','Þ','ç','Ç','ö','Ö','ü','Ü' });
        foreach (var ch in text)
            if (trChars.Contains(ch)) return true;
        // Use Turkish-unique function words and common verbs; avoid shared proper nouns
        var trWords = new[] {
            "ve","bir","için","çünkü","ancak","fakat","ama","ile","þimdi","her","hiç",
            "nasýl","neden","deðil","deðildir","yok","var","daha","en","çok","az",
            "mesela","örneðin","bence","böyle","þöyle","burada","orada","nerede",
            "geldi","gidiyor","yapýyorum","yapýyoruz","konuþuyor","ediyorum","oluyor","olacak"
        };
        foreach (var w in trWords)
            if (Regex.IsMatch(text, $"\\b{Regex.Escape(w)}\\b", RegexOptions.IgnoreCase)) return true;
        return false;
    }

    public static bool IsLikelyRomanian(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var roChars = new HashSet<char>(new[] { '?','?','â','Â','î','Î','?','?','?','?' });
        foreach (var ch in text)
            if (roChars.Contains(ch)) return true;
        // Romanian-unique function words and common verbs; avoid shared proper nouns
        var roWords = new[] {
            "?i","este","sunt","în","din","cu","la","de","pe","care","fiecare",
            "nu","da","foarte","mai","cel","cea","acesta","aceasta","ace?tia","acelea",
            "merge","vine","vorbe?te","fac","facem","se","întâmpl?","va","fie"
        };
        foreach (var w in roWords)
            if (Regex.IsMatch(text, $"\\b{Regex.Escape(w)}\\b", RegexOptions.IgnoreCase)) return true;
        return false;
    }

    // Normalize common Romanian OCR quirks (e.g., wrong diacritics or quotes)
    public static string NormalizeRomanian(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text
            .Replace("`", "'")
            .Replace("”", "\"")
            .Replace("“", "\"")
            .Replace("„", "\"")
            .Replace("–", "-")
            .Replace("—", "-");
        return normalized;
    }

    private static int CountOccurrences(string text, string value)
    {
        var matches = Regex.Matches(text, $"\\b{Regex.Escape(value)}\\b", RegexOptions.IgnoreCase);
        return matches.Count;
    }
}
