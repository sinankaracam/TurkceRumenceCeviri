using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TurkceRumenceCeviri.Utilities
{
    public class LicenseValidationResult
    {
        public int TotalKeys { get; set; }
        public int MatchedCount { get; set; }
        public int UnmatchedCount { get; set; }
        public List<string> MatchedKeys { get; } = new List<string>();
        public List<string> UnmatchedKeys { get; } = new List<string>();
        public List<string> Messages { get; } = new List<string>();
    }

    public static class LicenseValidator
    {
        /// <summary>
        /// Validates plain license keys against a file that contains SHA-256 hashes (one per line, hex uppercase).
        /// Returns a LicenseValidationResult and emits human-readable messages via the optional messageHandler.
        /// Throws FileNotFoundException when any input file is missing.
        /// </summary>
        public static LicenseValidationResult ValidateKeysAgainstHashes(string keysFilePath, string hashesFilePath, Action<string>? messageHandler = null)
        {
            messageHandler ??= Console.WriteLine;

            if (string.IsNullOrWhiteSpace(keysFilePath)) throw new ArgumentException("keysFilePath is required", nameof(keysFilePath));
            if (string.IsNullOrWhiteSpace(hashesFilePath)) throw new ArgumentException("hashesFilePath is required", nameof(hashesFilePath));

            if (!File.Exists(keysFilePath))
            {
                var msg = $"Keys file not found: {keysFilePath}";
                messageHandler(msg);
                throw new FileNotFoundException(msg, keysFilePath);
            }

            if (!File.Exists(hashesFilePath))
            {
                var msg = $"Hashes file not found: {hashesFilePath}";
                messageHandler(msg);
                throw new FileNotFoundException(msg, hashesFilePath);
            }

            var result = new LicenseValidationResult();

            // Load hashes into a set for fast lookup. Normalize to uppercase and trim.
            var hashLines = File.ReadAllLines(hashesFilePath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim().ToUpperInvariant())
                .ToHashSet(StringComparer.Ordinal);

            var keyLines = File.ReadAllLines(keysFilePath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                .ToList();

            result.TotalKeys = keyLines.Count;

            using var sha = SHA256.Create();

            foreach (var key in keyLines)
            {
                var hash = ComputeSha256Hex(sha, key);
                if (hashLines.Contains(hash))
                {
                    result.MatchedKeys.Add(key);
                }
                else
                {
                    result.UnmatchedKeys.Add(key);
                }
            }

            result.MatchedCount = result.MatchedKeys.Count;
            result.UnmatchedCount = result.UnmatchedKeys.Count;

            // Prepare messages
            var summary = $"License validation completed. Total: {result.TotalKeys}, Matched: {result.MatchedCount}, Unmatched: {result.UnmatchedCount}.";
            result.Messages.Add(summary);
            messageHandler(summary);

            if (result.MatchedCount > 0)
            {
                var m = $"First {Math.Min(10, result.MatchedCount)} matched keys (sample):\n" + string.Join('\n', result.MatchedKeys.Take(10));
                result.Messages.Add(m);
                messageHandler(m);
            }

            if (result.UnmatchedCount > 0)
            {
                var u = $"First {Math.Min(20, result.UnmatchedCount)} unmatched keys (sample):\n" + string.Join('\n', result.UnmatchedKeys.Take(20));
                result.Messages.Add(u);
                messageHandler(u);
            }

            return result;
        }

        private static string ComputeSha256Hex(SHA256 sha, string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
