using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization; // Eklendi

namespace TurkceRumenceCeviri.Utilities
{
    public static class RomanianPhoneticConverter
    {
        private static readonly List<(string pattern, string replacement)> Rules = new()
        {
            (@"\bsw\b", "sv"), // extra rule? keeping to requested list mostly.
            // Request rules:
            // { reg: /ce/g, rep: 'çe' },
            // { reg: /ci/g, rep: 'çi' },
            // { reg: /ge/g, rep: 'ce' },
            // { reg: /gi/g, rep: 'ci' },
            // { reg: /che/g, rep: 'ke' },
            // { reg: /chi/g, rep: 'ki' },
            // { reg: /ghe/g, rep: 'ge' },
            // { reg: /ghi/g, rep: 'gi' },
            // { reg: /ț/g, rep: 'ts' },
            // { reg: /ș/g, rep: 'ş' },
            // { reg: /ă/g, rep: 'ı' },
            // { reg: /â|î/g, rep: 'ı' },
            // { reg: /\bea\b/g, rep: 'ya' },

            // Order matters! e.g. che before ce.
            (@"che", "ke"),
            (@"chi", "ki"),
            (@"ghe", "ge"),
            (@"ghi", "gi"),
            (@"ce", "çe"),
            (@"ci", "çi"),
            (@"ge", "ce"),
            (@"gi", "ci"),
            (@"ț", "ts"),
            (@"ş", "ş"), // Input might already satisfy this or not, Romanian s-comma vs s-cedilla. Standardize.
            (@"ș", "ş"), // s-comma
            (@"ţ", "ts"), // t-cedilla
            
            (@"ă", "ı"),
            (@"â", "ı"),
            (@"î", "ı"),
            (@"\bea\b", "ya"), // Regex for whole word 'ea'
            (@"\u021B", "ts"), // Safe representation for ț
            (@"sh", "ş")      // Example fix if standardizing
        };

        public static string Convert(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // ADIM 0: Sayı dönüşümü (Aynı kalıyor)
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
            // Küçük harfe çevirirken de dikkatli olmalıyız ama
            // ToLowerInvariant genelde güvenlidir çünkü kaynak Rumence (ASCII 'i' kullanır).
            string phonetic = text.ToLowerInvariant();

            // --- KURALLAR (Aynı kalıyor) ---
            // 1. Placeholder
            phonetic = Regex.Replace(phonetic, "ghe", "@@GE@@", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "ghi", "@@GI@@", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "che", "@@KE@@", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "chi", "@@KI@@", RegexOptions.IgnoreCase);

            // 2. Yumuşak dönüşümler
            phonetic = Regex.Replace(phonetic, "ce", "çe", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "ci", "çi", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "ge", "ce", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "gi", "ci", RegexOptions.IgnoreCase);

            // 3. Özel karakterler
            phonetic = Regex.Replace(phonetic, @"\u021B", "ts");
            phonetic = Regex.Replace(phonetic, @"\u0163", "ts");
            phonetic = Regex.Replace(phonetic, @"\u0219", "ş");
            phonetic = Regex.Replace(phonetic, @"\u015F", "ş");
            phonetic = Regex.Replace(phonetic, @"\u0103", "ı");
            phonetic = Regex.Replace(phonetic, @"\u00E2", "ı");
            phonetic = Regex.Replace(phonetic, @"\u00EE", "ı");

            // 4. Diğer
            phonetic = Regex.Replace(phonetic, "ea", "ya", RegexOptions.IgnoreCase);
            phonetic = Regex.Replace(phonetic, "c", "k", RegexOptions.IgnoreCase);

            // 5. Geri yükleme
            phonetic = phonetic.Replace("@@GE@@", "ge");
            phonetic = phonetic.Replace("@@GI@@", "gi");
            phonetic = phonetic.Replace("@@KE@@", "ke");
            phonetic = phonetic.Replace("@@KI@@", "ki");
            
            phonetic = phonetic.Replace("x", "ks");

            // KRİTİK DEĞİŞİKLİK BURADA:
            // Eskisi: return phonetic.ToUpperInvariant(); --> i'leri I yapar (noktasız).
            // Yenisi: Türkçe kültürüne göre büyütüyoruz --> i'ler İ olur (noktalı).
            // Böylece Türk okuyucu 'İ' görünce sesi doğru çıkarır.
            
            return phonetic.ToUpper(new CultureInfo("tr-TR"));
        }

        // Sayıyı Rumence yazıya çeviren yardımcı metot (Basitleştirilmiş 0-999999 desteği)
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
                // Rumence gramer: 1000 = o mie, 2000 = două mii
                long thousands = number / 1000;
                if (thousands == 1) words += "o mie ";
                else if (thousands == 2) words += "două mii ";
                else words += NumberToRomanian(thousands) + " mii ";
                
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                // 100 = o sută, 200 = două sute
                long hundreds = number / 100;
                if (hundreds == 1) words += "o sută ";
                else if (hundreds == 2) words += "două sute ";
                else words += NumberToRomanian(hundreds) + " sute ";
                
                number %= 100;
            }

            if (number > 0)
            {
                //if (words != "") words += "și "; // Genellikle 've' kullanılmaz, gerekirse açılabilir
                
                var unitsMap = new[] { "zero", "unu", "doi", "trei", "patru", "cinci", "șase", "șapte", "opt", "nouă", "zece", "unsprezece", "doisprezece", "treisprezece", "paisprezece", "cincisprezece", "șaisprezece", "șaptesprezece", "optsprezece", "nouăsprezece" };
                var tensMap = new[] { "zero", "zece", "douăzeci", "treizeci", "patruzeci", "cincizeci", "șaizeci", "șaptezeci", "optzeci", "nouăzeci" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += " și " + unitsMap[number % 10]; // Örn: douăzeci și unu
                }
            }

            return words.Trim();
        }
    }
}
