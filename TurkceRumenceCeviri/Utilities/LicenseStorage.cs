using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TurkceRumenceCeviri.Utilities
{
    public class PersistedSettings
    {
        public string TranslatorKey { get; set; } = "";
        public string SpeechKey { get; set; } = "";
        public string GroqKey { get; set; } = "";
        public string LicenseKey { get; set; } = "";
        public string DeviceCode { get; set; } = "";
        public string TranslatorRegion { get; set; } = "westeurope";
        public string SpeechRegion { get; set; } = "westeurope";
    }

    public static class LicenseStorage
    {
        // File layout (version 1): 4 bytes magic | 1 byte version | 32 bytes HMAC-SHA256 | payload (optionally DPAPI-protected JSON)
        private const string Magic = "TRCF";
        private const byte Version = 1;
        private static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TurkceRumenceCeviri");
        private static readonly string FilePath = Path.Combine(DirectoryPath, "user.settings.dat");

        // Save persisted settings with integrity (HMAC) and optional encryption (DPAPI CurrentUser)
        public static void Save(PersistedSettings s)
        {
            try
            {
                if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);

                var json = JsonSerializer.Serialize(s);
                var plainBytes = Encoding.UTF8.GetBytes(json);

                // Optionally protect with DPAPI for confidentiality
                var payload = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

                // Derive HMAC key from hardware id to bind file to device/user
                var hwid = "";
                try { hwid = HardwareIdProvider.GetHardwareId() ?? string.Empty; } catch { hwid = string.Empty; }
                using var sha = SHA256.Create();
                var hmacKey = sha.ComputeHash(Encoding.UTF8.GetBytes(hwid ?? string.Empty));

                using var hmac = new HMACSHA256(hmacKey);
                var mac = hmac.ComputeHash(payload);

                using var ms = new MemoryStream();
                // write header
                var magicBytes = Encoding.ASCII.GetBytes(Magic);
                ms.Write(magicBytes, 0, magicBytes.Length);
                ms.WriteByte(Version);
                // write mac (32 bytes)
                ms.Write(mac, 0, mac.Length);
                // write payload
                ms.Write(payload, 0, payload.Length);

                File.WriteAllBytes(FilePath, ms.ToArray());
                DebugHelper.LogMessage("Persisted settings saved to user.settings.dat", "LICENSE");
            }
            catch (Exception ex)
            {
                DebugHelper.LogError("Failed to save persisted settings", ex);
            }
        }

        public static PersistedSettings? Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var all = File.ReadAllBytes(FilePath);
                var offset = 0;
                if (all.Length < 4 + 1 + 32) // header + version + hmac
                {
                    DebugHelper.LogWarning("Persisted settings file is too small or corrupt.");
                    return null;
                }

                var magicBytes = Encoding.ASCII.GetBytes(Magic);
                for (int i = 0; i < magicBytes.Length; i++)
                {
                    if (all[i] != magicBytes[i])
                    {
                        DebugHelper.LogWarning("Persisted settings magic mismatch - file may be corrupt.");
                        return null;
                    }
                }
                offset += magicBytes.Length;

                var fileVersion = all[offset++];
                if (fileVersion != Version)
                {
                    DebugHelper.LogWarning($"Unsupported persisted settings version: {fileVersion}");
                    return null;
                }

                var mac = new byte[32];
                Array.Copy(all, offset, mac, 0, 32);
                offset += 32;

                var payload = new byte[all.Length - offset];
                Array.Copy(all, offset, payload, 0, payload.Length);

                // derive hmac key
                var hwid = "";
                try { hwid = HardwareIdProvider.GetHardwareId() ?? string.Empty; } catch { hwid = string.Empty; }
                using var sha = SHA256.Create();
                var hmacKey = sha.ComputeHash(Encoding.UTF8.GetBytes(hwid ?? string.Empty));

                using var hmac = new HMACSHA256(hmacKey);
                var expected = hmac.ComputeHash(payload);
                if (!CryptographicOperations.FixedTimeEquals(expected, mac))
                {
                    DebugHelper.LogWarning("Persisted settings HMAC validation failed (file tampered or wrong device).");
                    return null;
                }

                // Unprotect payload
                byte[] plain;
                try
                {
                    plain = ProtectedData.Unprotect(payload, null, DataProtectionScope.CurrentUser);
                }
                catch (Exception ex)
                {
                    DebugHelper.LogWarning($"Failed to unprotect persisted settings payload: {ex.Message}");
                    return null;
                }

                var json = Encoding.UTF8.GetString(plain);
                var obj = JsonSerializer.Deserialize<PersistedSettings>(json);
                DebugHelper.LogMessage("Loaded persisted settings from user.settings.dat", "LICENSE");
                return obj;
            }
            catch (Exception ex)
            {
                DebugHelper.LogError("Failed to load persisted settings", ex);
                return null;
            }
        }
    }
}
