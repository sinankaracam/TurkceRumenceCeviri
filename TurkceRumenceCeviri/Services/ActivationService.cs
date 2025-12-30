using System;
using System.Security.Cryptography;
using System.Text;

namespace TurkceRumenceCeviri.Services
{
    public class ActivationService
    {
        private readonly string _verifierSalt;

        public ActivationService(string verifierSalt = "VerifierSalt_v1")
        {
            _verifierSalt = verifierSalt;
        }

        // Generates an activation key for a given deviceCode using the same algorithm
        public string GenerateActivationKey(string deviceCode)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_verifierSalt));
            var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceCode));
            var hex = BitConverter.ToString(sig).Replace("-", "").ToUpperInvariant();
            var key = string.Join("-", new[] { hex.Substring(0, 8), hex.Substring(8, 8), hex.Substring(16, 8), hex.Substring(24, 8) });
            return key;
        }

        public bool ValidateActivationKey(string deviceCode, string activationKey)
        {
            var normalizedKey = (activationKey ?? string.Empty).Replace("-", "").ToUpperInvariant();
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_verifierSalt));
            var expectedSig = hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceCode));
            var expectedHex = BitConverter.ToString(expectedSig).Replace("-", "").Substring(0, 32).ToUpperInvariant();
            return expectedHex.StartsWith(normalizedKey);
        }

        // Convenience: persist activation details when activation succeeds
        public void PersistActivation(string deviceCode, string activationKey, string? translatorKey = null, string? speechKey = null, string? groqKey = null, string? translatorRegion = null, string? speechRegion = null)
        {
            try
            {
                var s = new TurkceRumenceCeviri.Utilities.PersistedSettings
                {
                    DeviceCode = deviceCode ?? string.Empty,
                    LicenseKey = activationKey ?? string.Empty,
                    TranslatorKey = translatorKey ?? string.Empty,
                    SpeechKey = speechKey ?? string.Empty,
                    GroqKey = groqKey ?? string.Empty,
                    TranslatorRegion = translatorRegion ?? "westeurope",
                    SpeechRegion = speechRegion ?? "westeurope"
                };
                TurkceRumenceCeviri.Utilities.LicenseStorage.Save(s);
                TurkceRumenceCeviri.Utilities.DebugHelper.LogMessage("Activation persisted to disk", "LICENSE");
            }
            catch (Exception ex)
            {
                TurkceRumenceCeviri.Utilities.DebugHelper.LogWarning($"Failed to persist activation: {ex.Message}");
            }
        }
    }
}
