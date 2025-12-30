using TurkceRumenceCeviri.Configuration;
using TurkceRumenceCeviri.Utilities;

namespace TurkceRumenceCeviri.Services.Implementations;

public class ConfigService : IConfigService
{
    public AzureConfig Current { get; private set; }

    public ConfigService()
    {
        LoadFromPersisted();
        Current = AzureConfig.LoadFromEnvironment();
        DebugHelper.LogMessage(Current.ToString(), "CONFIG");
    }

    public void Reload()
    {
        LoadFromPersisted();
        Current = AzureConfig.LoadFromEnvironment();
        DebugHelper.LogMessage("Configuration reloaded", "CONFIG");
    }

    public void Validate()
    {
        // AzureConfig.LoadFromEnvironment çaðrýsý zaten validate tetikliyor
    }

    private void LoadFromPersisted()
    {
        try
        {
            var persisted = LicenseStorage.Load();
            if (persisted == null)
            {
                DebugHelper.LogWarning("No persisted settings found (user.settings.dat).");
                return;
            }

            var hwid = HardwareIdProvider.GetHardwareId();
            var currentDevice = DeviceCodeGenerator.CreateDeviceCode(hwid);
            if (string.IsNullOrEmpty(persisted.DeviceCode) || !string.Equals(persisted.DeviceCode, currentDevice, StringComparison.Ordinal))
            {
                DebugHelper.LogWarning("Persisted settings found but device code does not match current device.");
                return;
            }

            var act = new TurkceRumenceCeviri.Services.ActivationService();
            if (string.IsNullOrEmpty(persisted.LicenseKey) || !act.ValidateActivationKey(currentDevice, persisted.LicenseKey))
            {
                DebugHelper.LogWarning("Persisted license missing or invalid for this device.");
                return;
            }

            // apply to environment so AzureConfig.LoadFromEnvironment picks them up
            if (!string.IsNullOrEmpty(persisted.TranslatorKey)) Environment.SetEnvironmentVariable("AZURE_TRANSLATOR_KEY", persisted.TranslatorKey);
            if (!string.IsNullOrEmpty(persisted.SpeechKey)) Environment.SetEnvironmentVariable("AZURE_SPEECH_KEY", persisted.SpeechKey);
            if (!string.IsNullOrEmpty(persisted.GroqKey)) Environment.SetEnvironmentVariable("GROQ_API_KEY", persisted.GroqKey);
            if (!string.IsNullOrEmpty(persisted.LicenseKey)) Environment.SetEnvironmentVariable("LISANS_KEY", persisted.LicenseKey);

            DebugHelper.LogMessage("Applied persisted settings from user.settings.dat (device matched).");
        }
        catch (Exception ex)
        {
            DebugHelper.LogWarning($"Failed to load persisted settings: {ex.Message}");
        }
    }
}
