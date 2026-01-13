using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using TurkceRumenceCeviri.Services;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace TurkceRumenceCeviri.Utilities
{
    public static class RomanianPhoneticConverter
    {
        private static readonly List<(string pattern, string replacement)> Rules = new()
        {
            (@"\bsw\b", "sv"),
            (@"che", "ke"),
            (@"chi", "ki"),
            (@"ghe", "ge"),
            (@"ghi", "gi"),
            (@"ce", "çe"),
            (@"ci", "çi"),
            (@"ge", "ce"),
            (@"gi", "ci"),
            (@"ț", "ts"),
            (@"ş", "ş"),
            (@"ș", "ş"),
            (@"ţ", "ts"),
            (@"ă", "ı"),
            (@"â", "ı"),
            (@"î", "ı"),
            (@"\bea\b", "ya"), // sadece tek başına "ea" kelimesi
            (@"\u021B", "ts"),
            (@"sh", "ş")
        };

        public static string Convert(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Sayı dönüşümü
            string processingText = Regex.Replace(text, @"\b\d+\b", match =>
            {
                string numberStr = match.Value;
                if (long.TryParse(numberStr, out long number))
                {
                    string romanianWord = NumberToRomanian(number);
                    string phoneticWord = ApplyPhoneticRules(romanianWord);
                    return $"{numberStr} ({phoneticWord})";
                }
                return numberStr;
            });

            return ApplyPhoneticRules(processingText);
        }

        private static string ApplyPhoneticRules(string text)
        {
            string phonetic = text.ToLowerInvariant();

            // 1. Yer tutucular
            phonetic = Regex.Replace(phonetic, "ghe", "@@GE@@", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "ghi", "@@GI@@", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "che", "@@KE@@", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "chi", "@@KI@@", RegexOptions.IgnoreCase);

            // 2. Yumuşak sessizler
            phonetic = Regex.Replace(phonetic, "ce", "çe", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "ci", "çi", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "ge", "ce", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "gi", "ci", RegexOptions.IgnoreCase);

            // 3. Diakritikler
            phonetic = Regex.Replace(phonetic, @"\u021B", "ts"); // ț
            phonetic = Regex.Replace(phonetic, @"\u0163", "ts"); // ţ
            phonetic = Regex.Replace(phonetic, @"\u0219", "ş"); // ș
            phonetic = Regex.Replace(phonetic, @"\u015F", "ş"); // ş
            phonetic = Regex.Replace(phonetic, @"\u0103", "ı"); // ă
            phonetic = Regex.Replace(phonetic, @"\u00E2", "ı"); // â
            phonetic = Regex.Replace(phonetic, @"\u00EE", "ı"); // î

            // 4. 'ea' dönüşümü - sadece tek başına "ea" kelimesi
            phonetic = Regex.Replace(phonetic, @"\bea\b", "ya", RegexOptions.IgnoreCase);

            // 5. 'c' harfi → 'k' (yalnızca e/i ile devam etmeyen durumlarda)
            phonetic = Regex.Replace(phonetic, @"c(?![ei])", "k", RegexOptions.IgnoreCase);

            // 6. Yer tutucuları geri yükle
            phonetic = phonetic.Replace("@@GE@@", "ge");
            phonetic = phonetic.Replace("@@GI@@", "gi");
            phonetic = phonetic.Replace("@@KE@@", "ke");
            phonetic = phonetic.Replace("@@KI@@", "ki");

            // 7. 'x' → 'ks'
            phonetic = phonetic.Replace("x", "ks");

            // Türkçe kültürüne göre büyük harfe çevir
            return phonetic.ToUpper(new CultureInfo("tr-TR"));
        }

        private static string NumberToRomanian(long number)
        {
            if (number == 0) return "zero";
            if (number < 0) return "minus " + NumberToRomanian(Math.Abs(number));

            var words = "";

            if ((number / 1000000) > 0)
            {
                words += NumberToRomanian(number / 1000000) + " milioane ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                long thousands = number / 1000;
                if (thousands == 1) words += "o mie ";
                else if (thousands == 2) words += "două mii ";
                else words += NumberToRomanian(thousands) + " mii ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                long hundreds = number / 100;
                if (hundreds == 1) words += "o sută ";
                else if (hundreds == 2) words += "două sute ";
                else words += NumberToRomanian(hundreds) + " sute ";
                number %= 100;
            }

            if (number > 0)
            {
                var unitsMap = new[] { "zero", "unu", "doi", "trei", "patru", "cinci", "șase", "șapte", "opt", "nouă", "zece", "unsprezece", "doisprezece", "treisprezece", "paisprezece", "cincisprezece", "șaisprezece", "șaptesprezece", "optsprezece", "nouăsprezece" };
                var tensMap = new[] { "zero", "zece", "douăzeci", "treizeci", "patruzeci", "cincizeci", "șaizeci", "șaptezeci", "optzeci", "nouăzeci" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += " și " + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }
    }
}
