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
        var roChars = new HashSet<char>(new[] { 'ă','Ă','â','Â','î','Î','ț','Ț' });
        var trChars = new HashSet<char>(new[] { 'ğ','Ğ','ı','İ','ö','Ö','ü','Ü' });

        var hasRoDiacritic = false;
        var hasTrDiacritic = false;
        foreach (var ch in text)
        {
            if (roChars.Contains(ch)) { roScore += 3; hasRoDiacritic = true; }
            if (trChars.Contains(ch)) { trScore += 3; hasTrDiacritic = true; }
        }
        // Absolute rule kept minimal: presence of 'ț/Ț' strongly indicates Romanian
        if (text.IndexOf('ț') >= 0 || text.IndexOf('Ț') >= 0)
            return "ro";
        // Hard rule: if any Romanian diacritic appears, treat as Romanian (user preference)
        if (hasRoDiacritic && !hasTrDiacritic) return "ro";
        if (hasTrDiacritic && !hasRoDiacritic) return "tr";

        // Romanian common words (exclude ambiguous words like 'din')
        var roWords = new[] {
            // with diacritics
            "și","este","sunt","în","cu","la","de","ăsta","acesta","românia","bucurești",
            "bună","dimineața","cine","ești","mulțumesc","te","rog","da","nu","fereastră","coleg","grupă",
            // common OCR no-diacritic variants
            "si","este","sunt","in","cu","la","de","asta","acesta","romania","bucuresti",
            "buna","dimineata","cine","esti","multumesc","te","rog","da","nu","fereastra","coleg","grupa"
        };
        foreach (var w in roWords)
            roScore += CountOccurrences(text, w) * 2;

        // Turkish common words (exclude ambiguous 'de'/'da')
        var trWords = new[] { "ve", "bir", "için", "çok", "ama", "şu", "bu" };
        foreach (var w in trWords)
            trScore += CountOccurrences(text, w) * 2;

        // Majority decision by token counts (words with diacritics or in language-specific lists)
        static string NormalizeForRo(string s)
        {
            return s
                .Replace('ş','ș')
                .Replace('Ş','Ș')
                .Replace('ţ','ț')
                .Replace('Ţ','Ț')
                .ToLowerInvariant();
        }
        var tokens = System.Text.RegularExpressions.Regex.Matches(text, "\\p{L}+", RegexOptions.CultureInvariant);
        int roToken = 0, trToken = 0;
        var roWordSet = new HashSet<string>(roWords, StringComparer.OrdinalIgnoreCase);
        var trWordSet = new HashSet<string>(trWords, StringComparer.OrdinalIgnoreCase);
        foreach (System.Text.RegularExpressions.Match m in tokens)
        {
            var t = m.Value;
            var tLower = t.ToLowerInvariant();
            var tRo = NormalizeForRo(t);
            bool roHit = false;
            bool trHit = false;
            // diacritics based token hit
            foreach (var ch in t)
            {
                if (roChars.Contains(ch)) { roHit = true; break; }
            }
            if (!roHit)
            {
                // word list hit (handle OCR variants via NormalizeForRo)
                if (roWordSet.Contains(tRo)) roHit = true;
            }
            // Turkish hit: diacritics or word list
            foreach (var ch in t)
            {
                if (trChars.Contains(ch)) { trHit = true; break; }
            }
            if (!trHit)
            {
                if (trWordSet.Contains(tLower)) trHit = true;
            }
            if (roHit && !trHit) roToken++;
            else if (trHit && !roHit) trToken++;
        }
        if (roToken != trToken)
            return roToken > trToken ? "ro" : "tr";

        // Heuristics: punctuation patterns
        if (Regex.IsMatch(text, "\\b(Ç|İ)[a-z]")) trScore += 1; // Turkish uppercase with accents (exclude Ş to avoid overlap with Ș)
        if (Regex.IsMatch(text, "\\b(Ș|Ț|Ă|Â|Î)[a-z]")) roScore += 1; // Romanian uppercase with accents

        if (roScore != trScore) return roScore > trScore ? "ro" : "tr";

        // Fallback: frequency heuristic based on language-specific letters
        int roFreq = 0, trFreq = 0;
        foreach (var ch in text)
        {
            if (roChars.Contains(ch)) roFreq++;
            if (trChars.Contains(ch)) trFreq++;
        }
        // Strong majority rule to avoid single stray characters tipping the scale
        if (roFreq >= 2 && trFreq <= 1) return "ro";
        if (trFreq >= 2 && roFreq <= 1) return "tr";
        if (roFreq != trFreq) return roFreq > trFreq ? "ro" : "tr";

        // Last resort: default to Turkish
        return "tr";
    }

    public static bool IsLikelyTurkish(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var trChars = new HashSet<char>(new[] { 'ğ','Ğ','ı','İ','ç','Ç','ö','Ö','ü','Ü' });
        foreach (var ch in text)
            if (trChars.Contains(ch)) return true;
        // Use Turkish-unique function words and common verbs; avoid shared proper nouns
        var trWords = new[] {
            "ve","bir","için","çünkü","ancak","fakat","ama","ile","şimdi","her","hiç",
            "nasıl","neden","değil","değildir","yok","var","daha","en","çok","az",
            "mesela","örneğin","bence","böyle","şöyle","burada","orada","nerede",
            "geldi","gidiyor","yapıyorum","yapıyoruz","konuşuyor","ediyorum","oluyor","olacak"
        };
        foreach (var w in trWords)
            if (Regex.IsMatch(text, $"\\b{Regex.Escape(w)}\\b", RegexOptions.IgnoreCase)) return true;
        return false;
    }

    public static bool IsLikelyRomanian(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var roChars = new HashSet<char>(new[] { 'ă','Ă','â','Â','î','Î','ș','Ș','ț','Ț' });
        foreach (var ch in text)
            if (roChars.Contains(ch)) return true;
        // Romanian-unique function words and common verbs; avoid shared proper nouns
        var roWords = new[] {
            "și","este","sunt","în","din","cu","la","de","pe","care","fiecare",
            "nu","da","foarte","mai","cel","cea","acesta","aceasta","aceștia","acelea",
            "merge","vine","vorbește","fac","facem","se","întâmplă","va","fie"
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
